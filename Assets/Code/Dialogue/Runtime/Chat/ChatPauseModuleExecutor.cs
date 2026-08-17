using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="ChatPauseModule"/>.
/// Pausa silenciosa antes de continuar al siguiente módulo del nodo.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatPauseModuleExecutor : ModuleExecutorBase
{
    public override Type ModuleType => typeof(ChatPauseModule);

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (ChatPauseModule)module;
        if (m.duration > 0f)
            await Task.Delay(TimeSpan.FromSeconds(m.duration), ctx.Token);
        return null;
    }
}
