using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="EventDispatcherModule"/>.
/// Dispara un evento global con payload tipado via <see cref="GlobalDialogueEvents"/>.
/// La Task completa inmediatamente (el evento es síncrono).
/// </summary>
[DisallowMultipleComponent]
public sealed class EventModuleExecutor : ModuleExecutorBase
{
    public override Type ModuleType => typeof(EventDispatcherModule);

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (EventDispatcherModule)module;

        if (!string.IsNullOrEmpty(m.eventKey))
        {
            GlobalDialogueEvents.Fire(m.BuildPayload());
        }

        return Task.FromResult<string>(null);
    }
}
