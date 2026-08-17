using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Orquesta la ejecución de un <see cref="DialogueGraph"/> en el sistema de chat.
/// No conoce el contenido de los módulos: itera y despacha mediante un diccionario
/// de handlers registrados. Para añadir soporte a un módulo nuevo: una línea en
/// <see cref="RegisterHandlers"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatRunner : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private DialogueGraph autoStartGraph;

    [Header("Referencias")]
    [SerializeField] private GraphNavigator navigator;
    [SerializeField] private MonoBehaviour  presenterRef;
    [SerializeField] private MonoBehaviour  conditionEvaluatorRef;

    private IChatPresenter          _presenter;
    private IConditionEvaluator     _conditionEvaluator;
    private CancellationTokenSource _cts;
    private CharacterDefinition     _currentProfile;

    private readonly Dictionary<Type, Func<IDialogueModule, ChatExecutionContext, CancellationToken, Task<string>>> _handlers = new();
    private readonly List<ChatEntry> _history = new();

    public IReadOnlyList<ChatEntry> History => _history;
    public event Action OnChatComplete;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        _presenter          = presenterRef as IChatPresenter;
        _conditionEvaluator = conditionEvaluatorRef as IConditionEvaluator;
        if (_presenter == null) { enabled = false; return; }

        RegisterHandlers();
    }

    private void Start()
    {
        if (autoStartGraph != null)
            StartChat(autoStartGraph);
    }

    private void OnDestroy() => Stop();

    // -----------------------------------------------------------------------

    public void StartChat(DialogueGraph graph)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _history.Clear();
        _currentProfile = null;
        _presenter?.Clear();
        navigator.Init(graph);
        _ = RunAsync(navigator.StartNode(), _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // -----------------------------------------------------------------------

    private async Task RunAsync(DialogueNodeData startNode, CancellationToken ct)
    {
        var current = startNode;
        try
        {
            while (current != null)
            {
                ct.ThrowIfCancellationRequested();
                var nextPort = await ProcessNodeAsync(current, ct);
                current = navigator.NextFrom(current, nextPort);
            }
            OnChatComplete?.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { Debug.LogException(e); }
    }

    private async Task<string> ProcessNodeAsync(DialogueNodeData node, CancellationToken ct)
    {
        var ctx = new ChatExecutionContext(_presenter, _conditionEvaluator, _history, _currentProfile);

        foreach (var module in node.modules)
        {
            ct.ThrowIfCancellationRequested();
            if (!_handlers.TryGetValue(module.GetType(), out var handler)) continue;

            var port = await handler(module, ctx, ct);
            if (port != null)
            {
                _currentProfile = ctx.CurrentProfile;
                return port;
            }
        }

        _currentProfile = ctx.CurrentProfile;
        return "Next";
    }

    // -----------------------------------------------------------------------

    private void RegisterHandlers()
    {
        Register<ProfileModule>((m, ctx, ct) =>
        {
            ctx.CurrentProfile = m.profile;
            return Task.FromResult<string>(null);
        });

        Register<TextModule>((m, ctx, ct) =>
        {
            var entry = ChatEntry.ForText(m.speakerName, m.text, m.isOwn, ctx.CurrentProfile?.avatarSprite);
            ctx.History.Add(entry);
            ctx.Presenter?.AddMessage(entry);
            return Task.FromResult<string>(null);
        });

        Register<EmojiModule>((m, ctx, ct) =>
        {
            if (m.emoji == null) return Task.FromResult<string>(null);
            var entry = ChatEntry.ForEmoji(ctx.CurrentProfile?.displayName, m.emoji, m.isOwn, ctx.CurrentProfile?.avatarSprite);
            ctx.History.Add(entry);
            ctx.Presenter?.AddMessage(entry);
            return Task.FromResult<string>(null);
        });

        Register<ImageModule>((m, ctx, ct) =>
        {
            if (m.image == null) return Task.FromResult<string>(null);
            var entry = ChatEntry.ForImage(ctx.CurrentProfile?.displayName, m.image, m.isOwn, ctx.CurrentProfile?.avatarSprite);
            ctx.History.Add(entry);
            ctx.Presenter?.AddMessage(entry);
            return Task.FromResult<string>(null);
        });

        Register<EventDispatcherModule>((m, ctx, ct) =>
        {
            GlobalDialogueEvents.Fire(m.BuildPayload());
            return Task.FromResult<string>(null);
        });

        Register<ChatTypingModule>(async (m, ctx, ct) =>
        {
            if (ctx.Presenter != null)
                await ctx.Presenter.ShowTypingAsync(ctx.CurrentProfile, m.duration, ct);
            return null;
        });

        Register<ChatPauseModule>(async (m, ctx, ct) =>
        {
            if (m.duration > 0f)
                await Task.Delay(TimeSpan.FromSeconds(m.duration), ct);
            return null;
        });

        Register<WaitForSignalModule>(async (m, ctx, ct) =>
        {
            if (m.signal != null)
                await WaitForSignalAsync(m.signal, ct);
            return null;
        });

        Register<ChoiceModule>(async (m, ctx, ct) =>
        {
            var visible = ctx.ConditionEvaluator != null
                ? m.choices.FindAll(ctx.ConditionEvaluator.IsAllowed)
                : m.choices;
            var idx = ctx.Presenter != null
                ? await ctx.Presenter.ShowChoicesAsync(visible, ctx.CurrentProfile, ct)
                : 0;
            return visible.Count > idx ? visible[idx].portName : "Next";
        });

        Register<ImageChoiceModule>(async (m, ctx, ct) =>
        {
            var idx = ctx.Presenter != null
                ? await ctx.Presenter.ShowImageChoicesAsync(m.choices, ctx.CurrentProfile, ct)
                : 0;
            return m.choices.Count > idx ? m.choices[idx].portName : "Next";
        });
    }

    private void Register<TModule>(Func<TModule, ChatExecutionContext, CancellationToken, Task<string>> handler)
        where TModule : IDialogueModule
        => _handlers[typeof(TModule)] = (m, ctx, ct) => handler((TModule)m, ctx, ct);

    private static Task WaitForSignalAsync(SignalSO signal, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return Task.FromCanceled(ct);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action handler = null;
        CancellationTokenRegistration reg = default;

        handler = () =>
        {
            reg.Dispose();
            signal.OnRaised -= handler;
            tcs.TrySetResult(true);
        };

        signal.OnRaised += handler;

        reg = ct.Register(() =>
        {
            signal.OnRaised -= handler;
            tcs.TrySetCanceled();
        });

        return tcs.Task;
    }
}
