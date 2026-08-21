using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// IChatPresenter propio de una conversacion, usado durante toda su vida (no se
/// reasigna en los executors). Mientras no haya una UI real conectada (Attach),
/// guarda los mensajes en el historial de la conversacion y la marca como no
/// leida en vez de dibujar nada. En cuanto se conecta una UI real, todo lo nuevo
/// se reenvia ahi en directo.
///
/// Un Choice/ImageChoice necesita a un jugador presente: si llega en segundo
/// plano, se queda esperando (sin coste) a que se conecte una UI real antes de
/// devolver nada, en vez de intentar adivinar o saltarselo.
/// </summary>
public sealed class ConversationChatPresenter : IChatPresenter
{
    private readonly ChatConversation _conversation;
    private IChatPresenter _live; // null = en segundo plano, sin UI conectada

    // Se renueva en cada Attach: cancela cualquier llamada en vivo (p.ej. una burbuja
    // de Choice ya instanciada) que dependiera de la UI anterior, para que se
    // destruya limpiamente en vez de quedar huerfana cuando se desconecta.
    private CancellationTokenSource _liveCts;

    private event Action OnLiveAttached;

    public ConversationChatPresenter(ChatConversation conversation)
    {
        _conversation = conversation;
    }

    /// <summary>Conecta (o desconecta, con null) la UI real que muestra esta conversacion ahora mismo.</summary>
    public void Attach(IChatPresenter live)
    {
        _liveCts?.Cancel();
        _liveCts?.Dispose();
        _liveCts = live != null ? new CancellationTokenSource() : null;

        _live = live;
        if (live != null)
            OnLiveAttached?.Invoke();
    }

    public void AddMessage(ChatEntry entry)
    {
        _conversation.AppendHistory(entry);

        if (_live != null)
            _live.AddMessage(entry);
        else
            _conversation.MarkUnread();
    }

    public Task ShowTypingAsync(CharacterDefinition speaker, float seconds, CancellationToken ct)
        => _live != null ? _live.ShowTypingAsync(speaker, seconds, ct) : Task.CompletedTask;

    public Task<int> ShowChoicesAsync(IReadOnlyList<ChoiceModule.ChoiceData> choices, CharacterDefinition speaker, CancellationToken ct)
        => RunWhileLiveAsync((live, liveCt) => live.ShowChoicesAsync(choices, speaker, liveCt), ct);

    public Task<int> ShowImageChoicesAsync(IReadOnlyList<ImageChoiceModule.ImageChoiceData> choices, CharacterDefinition speaker, CancellationToken ct)
        => RunWhileLiveAsync((live, liveCt) => live.ShowImageChoicesAsync(choices, speaker, liveCt), ct);

    /// <summary>
    /// Pide una eleccion a la UI en vivo actual y espera su resultado. Si el jugador
    /// sale de la pantalla antes de elegir, la burbuja en curso se cancela (se
    /// destruye limpio) y se vuelve a pedir desde cero en cuanto haya UI conectada
    /// de nuevo, en vez de quedarse esperando para siempre una burbuja ya destruida.
    /// </summary>
    private async Task<int> RunWhileLiveAsync(Func<IChatPresenter, CancellationToken, Task<int>> request, CancellationToken ct)
    {
        while (true)
        {
            var live    = await WaitForLiveAsync(ct);
            var liveCts = _liveCts;
            if (liveCts == null) continue; // se desconecto de nuevo antes de que llegaramos aqui

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, liveCts.Token);
            try
            {
                return await request(live, linked.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Se desconecto la UI antes de que el jugador eligiera: reintentar con la siguiente.
            }
        }
    }

    public void Clear() => _live?.Clear();

    /// <summary>Espera (sin coste) a que se conecte una UI real, igual que WaitForSignalModuleExecutor espera una SignalSO.</summary>
    private Task<IChatPresenter> WaitForLiveAsync(CancellationToken ct)
    {
        if (_live != null) return Task.FromResult(_live);
        if (ct.IsCancellationRequested) return Task.FromCanceled<IChatPresenter>(ct);

        var tcs = new TaskCompletionSource<IChatPresenter>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action handler = null;
        CancellationTokenRegistration reg = default;

        handler = () =>
        {
            reg.Dispose();
            OnLiveAttached -= handler;
            tcs.TrySetResult(_live);
        };

        OnLiveAttached += handler;

        reg = ct.Register(() =>
        {
            OnLiveAttached -= handler;
            tcs.TrySetCanceled();
        });

        return tcs.Task;
    }
}
