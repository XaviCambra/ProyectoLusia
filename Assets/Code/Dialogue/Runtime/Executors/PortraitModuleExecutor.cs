using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="PortraitModule"/>.
/// Delega en <see cref="IPortraitController"/> para gestionar retratos.
/// </summary>
[DisallowMultipleComponent]
public sealed class PortraitModuleExecutor : MonoBehaviour, IModuleExecutor
{
    [SerializeField] private MonoBehaviour portraitControllerRef; // IPortraitController

    private IPortraitController _controller;
    private PortraitModule      _activeModule;

    public Type ModuleType => typeof(PortraitModule);

    private void Awake()
    {
        _controller = portraitControllerRef as IPortraitController;
        if (_controller == null)
            Debug.LogError("[PortraitModuleExecutor] Falta IPortraitController.");
    }

    public void Initialize(DialogueGraph graph)
    {
        if (_controller == null)
            _controller = portraitControllerRef as IPortraitController;
        _controller?.Init(graph);
    }

    public void OnNodeBegin() { }

    public async Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_controller == null) return;
        var m = (PortraitModule)module;
        _activeModule = m;
        try   { await _controller.ApplyAsync(m); }
        finally { _activeModule = null; }
    }

    public void Cancel() { }

    public bool TryFastForward()
    {
        if (_activeModule == null || _controller == null) return false;
        _controller.SnapPlacement(_activeModule);
        return true;
    }

    /// <summary>
    /// Detiene y limpia todos los retratos activos.
    /// Llamado por el runner al finalizar el diálogo.
    /// </summary>
    public void ResetAll() => _controller?.ResetAll();
}
