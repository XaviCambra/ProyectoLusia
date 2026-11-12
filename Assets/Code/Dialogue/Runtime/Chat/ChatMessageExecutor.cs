using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Base comun de los executors de chat que solo anaden un mensaje (Text, Emoji, Image).
/// Guarda el presenter (<see cref="IPresenterHost"/>) y delega la conversion
/// modulo -> <see cref="ChatEntry"/> en <see cref="ChatEntryFactory"/>, que es la
/// unica fuente de verdad de como se ve cada mensaje.
/// Para un tipo nuevo de mensaje: anadir su caso en ChatEntryFactory y una subclase
/// vacia <c>public sealed class XModuleExecutor : ChatMessageExecutor&lt;XModule&gt; { }</c>.
/// </summary>
public abstract class ChatMessageExecutor<TModule> : ModuleExecutorBase, IPresenterHost
    where TModule : IDialogueModule
{
    [SerializeField] private MonoBehaviour presenterRef; // IChatPresenter

    private IChatPresenter _presenter;

    public override Type ModuleType => typeof(TModule);

    protected virtual void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
    }

    public void SetPresenter(IChatPresenter presenter) => _presenter = presenter;

    public override Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (!ChatEntryFactory.TryCreate(module, ctx.CurrentProfile, out var entry))
            return Task.FromResult<string>(null);

        if (_presenter == null)
        {
            Debug.LogError($"[{GetType().Name}] Nodo '{ctx.NodeGuid}': no hay IChatPresenter asignado, el mensaje se pierde.", this);
            return Task.FromResult<string>(null);
        }

        _presenter.AddMessage(entry);
        return Task.FromResult<string>(null);
    }
}
