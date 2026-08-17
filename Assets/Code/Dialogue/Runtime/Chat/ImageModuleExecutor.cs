using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="ImageModule"/>.
/// Añade una burbuja de imagen vía <see cref="IChatPresenter"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ImageModuleExecutor : ModuleExecutorBase
{
    [SerializeField] private MonoBehaviour presenterRef; // IChatPresenter

    private IChatPresenter _presenter;

    public override Type ModuleType => typeof(ImageModule);

    private void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
    }

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (ImageModule)module;
        if (m.image == null) return Task.FromResult<string>(null);

        var entry = ChatEntry.ForImage(ctx.CurrentProfile?.displayName, m.image, m.isOwn, ctx.CurrentProfile?.avatarSprite);
        _presenter?.AddMessage(entry);
        return Task.FromResult<string>(null);
    }
}
