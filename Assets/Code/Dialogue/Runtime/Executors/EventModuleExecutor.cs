using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="EventDispatcherModule"/>.
/// Dispara un evento global con payload tipado via <see cref="GlobalDialogueEvents"/>.
/// La Task completa inmediatamente (el evento es síncrono).
/// </summary>
[DisallowMultipleComponent]
public sealed class EventModuleExecutor : MonoBehaviour, IModuleExecutor
{
    public Type ModuleType => typeof(EventDispatcherModule);

    public void Initialize(DialogueGraph graph) { }
    public void OnNodeBegin() { }
    public void Cancel() { }
    public bool TryFastForward() => false;
    public void ResetAll() { }

    public Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (EventDispatcherModule)module;

        if (!string.IsNullOrEmpty(m.eventKey))
        {
            try
            {
                GlobalDialogueEvents.Fire(m.BuildPayload());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventModuleExecutor] Error disparando evento '{m.eventKey}': {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }
}
