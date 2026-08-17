using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="ChatTypingModule"/>.
/// Muestra el indicador "está escribiendo..." durante la duración indicada.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatTypingModuleExecutor : ModuleExecutorBase
{
    [SerializeField] private MonoBehaviour presenterRef; // IChatPresenter

    private IChatPresenter _presenter;

    public override Type ModuleType => typeof(ChatTypingModule);

    private void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
    }

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (ChatTypingModule)module;
        if (_presenter != null)
            await _presenter.ShowTypingAsync(ctx.CurrentProfile, m.duration, ctx.Token);
        return null;
    }
}
