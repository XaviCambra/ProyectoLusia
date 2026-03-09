using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Runner de chat: recorre un <see cref="DialogueGraph"/> procesando únicamente
/// <see cref="TextModule"/>, <see cref="ChoiceModule"/>, <see cref="EventDispatcherModule"/>,
/// <see cref="ChatPauseModule"/>, <see cref="ChatTypingModule"/> y <see cref="WaitForSignalModule"/>.
/// Acumula las entradas en <see cref="History"/> y delega la presentación
/// en un <see cref="IChatPresenter"/>.
///
/// No toca ni modifica <see cref="DialogueRunner"/>; comparte solo el grafo y el navegador.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatRunner : MonoBehaviour
{
    [SerializeField] private GraphNavigator   navigator;
    [SerializeField] private MonoBehaviour    presenterRef; // debe implementar IChatPresenter

    private IChatPresenter           _presenter;
    private CancellationTokenSource  _cts;

    private readonly List<ChatEntry> _history = new();

    /// <summary>Historial acumulado de mensajes desde el último <see cref="StartChat"/>.</summary>
    public IReadOnlyList<ChatEntry> History => _history;

    /// <summary>Se invoca cuando el grafo llega a su fin sin cancelación.</summary>
    public event Action OnChatComplete;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
        if (_presenter == null)
            Debug.LogError("[ChatRunner] presenterRef no implementa IChatPresenter.");
    }

    private void OnDestroy() => Stop();

    // -----------------------------------------------------------------------

    /// <summary>Inicia el recorrido del grafo desde su nodo de inicio.</summary>
    public void StartChat(DialogueGraph graph)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _history.Clear();
        _presenter?.Clear();
        navigator.Init(graph);
        _ = RunAsync(navigator.StartNode(), _cts.Token);
    }

    /// <summary>Cancela la ejecución en curso.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // -----------------------------------------------------------------------

    private async Task RunAsync(DialogueNodeData startNode, CancellationToken ct)
    {
        var current = startNode;
        try
        {
            while (current != null)
            {
                ct.ThrowIfCancellationRequested();

                var nextPort = await ProcessNodeAsync(current, ct);
                current = navigator.NextFrom(current, nextPort);
            }

            OnChatComplete?.Invoke();
        }
        catch (OperationCanceledException) { /* cancelación normal */ }
        catch (Exception e)
        {
            Debug.LogError($"[ChatRunner] Error inesperado: {e}");
        }
    }

    /// <summary>
    /// Procesa todos los módulos del nodo y devuelve el puerto de salida a usar.
    /// </summary>
    private async Task<string> ProcessNodeAsync(DialogueNodeData node, CancellationToken ct)
    {
        var nextPort  = "Next";
        var breakLoop = false;

        foreach (var module in node.modules)
        {
            ct.ThrowIfCancellationRequested();

            switch (module)
            {
                case TextModule m:
                    var entry = new ChatEntry(m.speakerName, m.text);
                    _history.Add(entry);
                    _presenter?.AddMessage(entry);
                    break;

                case ChoiceModule m:
                    var idx = _presenter != null
                        ? await _presenter.ShowChoicesAsync(m.choices, ct)
                        : 0;
                    nextPort  = (m.choices.Count > idx) ? m.choices[idx].portName : "Next";
                    breakLoop = true; // el choice decide la salida; ignorar módulos posteriores
                    break;

                case EventDispatcherModule m:
                    GlobalDialogueEvents.Fire(m.BuildPayload());
                    break;

                case ChatTypingModule m when _presenter != null:
                    await _presenter.ShowTypingAsync(m.profile, m.duration, ct);
                    break;

                case ChatPauseModule m when m.duration > 0f:
                    await Task.Delay(TimeSpan.FromSeconds(m.duration), ct);
                    break;

                case WaitForSignalModule m when m.signal != null:
                    await WaitForSignalAsync(m.signal, ct);
                    break;
            }

            if (breakLoop) break;
        }

        return nextPort;
    }

    /// <summary>
    /// Espera de forma asíncrona hasta que <paramref name="signal"/> emita,
    /// o hasta que <paramref name="ct"/> sea cancelado.
    /// </summary>
    private static Task WaitForSignalAsync(SignalSO signal, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled(ct);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Action handler = null;
        CancellationTokenRegistration reg = default;

        handler = () =>
        {
            reg.Dispose();              // limpia el registro de ct
            signal.OnRaised -= handler; // se desuscribe a sí mismo
            tcs.TrySetResult(true);
        };

        signal.OnRaised += handler;

        reg = ct.Register(() =>
        {
            signal.OnRaised -= handler;
            tcs.TrySetCanceled();
        });

        return tcs.Task;
    }
}
