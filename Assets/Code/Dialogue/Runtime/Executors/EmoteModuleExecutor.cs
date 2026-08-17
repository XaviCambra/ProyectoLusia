using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="EmoteModule"/>.
/// Delega en <see cref="IPortraitController"/> para reproducir o detener emotes.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmoteModuleExecutor : ModuleExecutorBase
{
    [SerializeField] private MonoBehaviour portraitControllerRef; // IPortraitController

    private IPortraitController _controller;

    public override Type ModuleType => typeof(EmoteModule);

    private void Awake()
    {
        _controller = portraitControllerRef as IPortraitController;
    }

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_controller == null) return Task.FromResult<string>(null);
        var m = (EmoteModule)module;

        if (m.clip != null)
            _controller.PlaySpecialAnimation(m.profileRef, m.clip, m.speed, m.loop, m.persistent);
        else
            _controller.StopSpecialAnimation(m.profileRef);

        return Task.FromResult<string>(null);
    }
}
