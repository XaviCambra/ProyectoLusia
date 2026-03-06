using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="EmoteModule"/>.
/// Delega en <see cref="IPortraitController"/> para reproducir o detener emotes.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmoteModuleExecutor : MonoBehaviour, IModuleExecutor
{
    [SerializeField] private MonoBehaviour portraitControllerRef; // IPortraitController

    private IPortraitController _controller;

    public Type ModuleType => typeof(EmoteModule);

    private void Awake()
    {
        _controller = portraitControllerRef as IPortraitController;
        if (_controller == null)
            Debug.LogError("[EmoteModuleExecutor] Falta IPortraitController.");
    }

    public void Initialize(DialogueGraph graph)
    {
        if (_controller == null)
            _controller = portraitControllerRef as IPortraitController;
        // No necesita Init(graph) — no crea GameObjects propios.
    }

    public void OnNodeBegin() { }

    public Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_controller == null) return Task.CompletedTask;
        var m = (EmoteModule)module;

        if (m.stopAnimation)
            _controller.StopSpecialAnimation(m.ProfileId);
        else if (m.clip != null)
            _controller.PlaySpecialAnimation(m.ProfileId, m.clip, m.speed, m.loop);

        return Task.CompletedTask;
    }

    public void Cancel() { }
    public bool TryFastForward() => false;

    /// <summary>
    /// No-op: el cleanup de PlayableGraphs lo realiza PortraitController.ResetAll().
    /// </summary>
    public void ResetAll() { }
}
