using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. El homologo de Retratos es TextModuleExecutor.
/// <summary>
/// Executor para <see cref="TextModule"/> en el motor de Chat.
/// Añade una burbuja de texto vía <see cref="IChatPresenter"/>. A diferencia de
/// <see cref="TextModuleExecutor"/> (Retratos), no usa typewriter ni localización:
/// el chat muestra el mensaje completo de una vez, como una burbuja.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatTextModuleExecutor : ModuleExecutorBase, IPresenterHost
{
    [SerializeField] private MonoBehaviour presenterRef; // IChatPresenter

    private IChatPresenter _presenter;

    public override Type ModuleType => typeof(TextModule);

    private void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
    }

    public void SetPresenter(IChatPresenter presenter) => _presenter = presenter;

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (TextModule)module;
        var entry = ChatEntry.ForText(m.speakerName, m.text, m.isOwn, ctx.CurrentProfile?.avatarSprite);
        _presenter?.AddMessage(entry);
        return Task.FromResult<string>(null);
    }
}
