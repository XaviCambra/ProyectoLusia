using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="WaitForSignalModule"/>.
/// Detiene la ejecución del nodo hasta que se dispare la <see cref="SignalSO"/> configurada,
/// o hasta que se cancele el nodo (navegación, fin de diálogo).
/// </summary>
[DisallowMultipleComponent]
public sealed class WaitForSignalModuleExecutor : ModuleExecutorBase
{
    public override Type ModuleType => typeof(WaitForSignalModule);

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (WaitForSignalModule)module;
        if (m.signal != null)
            await WaitForSignalAsync(m.signal, ctx.Token);
        return null;
    }

    private static Task WaitForSignalAsync(SignalSO signal, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return Task.FromCanceled(ct);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action handler = null;
        CancellationTokenRegistration reg = default;

        handler = () =>
        {
            reg.Dispose();
            signal.OnRaised -= handler;
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
