using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="ProfileModule"/>.
/// Fija el personaje "actual" en el contexto para que los módulos siguientes
/// del mismo nodo (Text, Emoji, Image, Choice...) lo usen como hablante/avatar.
/// No produce salida visual propia ni decide navegación.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProfileModuleExecutor : ModuleExecutorBase
{
    public override Type ModuleType => typeof(ProfileModule);

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (ProfileModule)module;
        ctx.CurrentProfile = m.profile;
        return Task.FromResult<string>(null);
    }
}
