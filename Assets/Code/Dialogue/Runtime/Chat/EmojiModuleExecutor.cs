using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="EmojiModule"/>.
/// Añade una burbuja de emoji vía <see cref="IChatPresenter"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmojiModuleExecutor : ModuleExecutorBase, IPresenterHost
{
    [SerializeField] private MonoBehaviour presenterRef; // IChatPresenter

    private IChatPresenter _presenter;

    public override Type ModuleType => typeof(EmojiModule);

    private void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
    }

    public void SetPresenter(IChatPresenter presenter) => _presenter = presenter;

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (EmojiModule)module;
        if (m.emoji == null) return Task.FromResult<string>(null);

        var entry = ChatEntry.ForEmoji(ctx.CurrentProfile?.displayName, m.emoji, m.isOwn, ctx.CurrentProfile?.avatarSprite);
        _presenter?.AddMessage(entry);
        return Task.FromResult<string>(null);
    }
}
