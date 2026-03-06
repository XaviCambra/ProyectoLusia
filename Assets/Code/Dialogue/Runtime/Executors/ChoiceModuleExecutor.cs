using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="ChoiceModule"/>.
/// Muestra las opciones de elección y espera a que el jugador seleccione una.
/// La Task completa cuando el jugador elige, devolviendo el nombre del puerto seleccionado
/// a través de <see cref="OnChoiceSelected"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChoiceModuleExecutor : MonoBehaviour, IModuleExecutor
{
    [SerializeField] private MonoBehaviour choiceUIRef;   // IChoiceUIController

    private IChoiceUIController _choiceUI;
    private TaskCompletionSource<bool> _waitTcs;

    /// <summary>
    /// El runner se suscribe a este evento para recibir el puerto seleccionado
    /// y navegar al nodo correspondiente.
    /// </summary>
    public event Action<string> OnChoiceSelected;

    public Type ModuleType => typeof(ChoiceModule);

    private void Awake()
    {
        _choiceUI = choiceUIRef as IChoiceUIController;
        if (_choiceUI == null)
            Debug.LogError("[ChoiceModuleExecutor] Falta IChoiceUIController.");
    }

    public void Initialize(DialogueGraph graph) { }

    public void OnNodeBegin()
    {
        _choiceUI?.BeginNode();
    }

    public async Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_choiceUI == null) return;

        var m = (ChoiceModule)module;
        _waitTcs = new TaskCompletionSource<bool>();

        bool shown = _choiceUI.Show(m, ctx.NodeGuid, port =>
        {
            _choiceUI.Hide();
            OnChoiceSelected?.Invoke(port);
            _waitTcs.TrySetResult(true);
        });

        if (!shown)
        {
            _waitTcs.TrySetResult(true);
            return;
        }

        // Esperar a que el usuario elija (o a cancelación)
        await Task.WhenAny(_waitTcs.Task, ctx.Token.AsTask());
    }

    public void Cancel()
    {
        _waitTcs?.TrySetCanceled();
        _choiceUI?.Hide();
    }

    public bool TryFastForward() => false;
    public void ResetAll() => Cancel();

    /// <summary>
    /// Intenta consumir una tecla numérica (1-4) como hotkey de elección.
    /// Devuelve true si se consumió una elección.
    /// </summary>
    public bool TryConsumeHotkey(out string chosenPort)
    {
        if (_choiceUI != null && _choiceUI.TryConsumeHotkey(out chosenPort))
        {
            _choiceUI.Hide();
            _waitTcs?.TrySetResult(true);
            OnChoiceSelected?.Invoke(chosenPort);
            return true;
        }
        chosenPort = null;
        return false;
    }
}

/// <summary>
/// Extensión para convertir CancellationToken en Task (para usar con WhenAny).
/// </summary>
internal static class CancellationTokenExtensions
{
    public static Task AsTask(this System.Threading.CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>();
        token.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }
}
