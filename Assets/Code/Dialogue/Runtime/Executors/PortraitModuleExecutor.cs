using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="PortraitModule"/>.
/// Delega en <see cref="IPortraitController"/> para gestionar retratos.
/// </summary>
[DisallowMultipleComponent]
public sealed class PortraitModuleExecutor : ModuleExecutorBase
{
    [SerializeField] private MonoBehaviour portraitControllerRef; // IPortraitController

    private IPortraitController _controller;
    private PortraitModule      _activeModule;

    public override Type ModuleType => typeof(PortraitModule);

    private void Awake()
    {
        _controller = portraitControllerRef as IPortraitController;
    }

    public override void Initialize(DialogueGraph graph)
    {
        // Awake de este executor puede no haber corrido aún si DialogueRunner.Awake va primero
        _controller ??= portraitControllerRef as IPortraitController;
        _controller?.Init(graph);
    }

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_controller == null) return null;
        var m = (PortraitModule)module;
        _activeModule = m;
        try   { await _controller.ApplyAsync(m); }
        finally { _activeModule = null; }
        return null;
    }

    public override bool TryFastForward()
    {
        if (_activeModule == null || _controller == null) return false;
        _controller.SnapPlacement(_activeModule);
        return true;
    }

    public override void ResetAll() => _controller?.ResetAll();
}
