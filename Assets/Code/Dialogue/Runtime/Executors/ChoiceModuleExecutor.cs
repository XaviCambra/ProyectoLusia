using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="ChoiceModule"/>.
/// Muestra las opciones de elección y espera a que el jugador seleccione una.
/// Devuelve el puerto elegido como resultado de <see cref="ExecuteAsync"/> (el
/// runner lo usa para navegar); no depende de ningún evento aparte.
/// Las hotkeys numéricas (1-4) se gestionan en el propio Update,
/// completando el mismo <see cref="TaskCompletionSource{T}"/> que el click.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChoiceModuleExecutor : ModuleExecutorBase
{
    [SerializeField] private MonoBehaviour choiceUIRef; // IChoiceUIController

    private IChoiceUIController _choiceUI;
    private TaskCompletionSource<string> _waitTcs;

    public override Type ModuleType => typeof(ChoiceModule);

    private void Awake()
    {
        _choiceUI = choiceUIRef as IChoiceUIController;
    }

    /// <summary>
    /// Sondea las hotkeys numéricas cada frame sin necesitar que el runner lo llame.
    /// </summary>
    private void Update()
    {
        if (_choiceUI == null) return;
        if (!_choiceUI.TryConsumeHotkey(out var chosenPort)) return;

        _choiceUI.Hide();
        _waitTcs?.TrySetResult(chosenPort);
    }

    public override void OnNodeBegin() => _choiceUI?.BeginNode();

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_choiceUI == null) return null;

        var m = (ChoiceModule)module;
        _waitTcs = new TaskCompletionSource<string>();

        bool shown = _choiceUI.Show(m, ctx.NodeGuid, port =>
        {
            _choiceUI.Hide();
            _waitTcs.TrySetResult(port);
        });

        if (!shown)
        {
            return null;
        }

        await Task.WhenAny(_waitTcs.Task, ctx.Token.AsTask());
        return _waitTcs.Task.Status == TaskStatus.RanToCompletion ? _waitTcs.Task.Result : null;
    }

    public override void Cancel()
    {
        _waitTcs?.TrySetCanceled();
        _choiceUI?.Hide();
    }

    public override void ResetAll() => Cancel();
}

/// <summary>
/// Extensión para convertir CancellationToken en Task (para usar con WhenAny).
/// </summary>
internal static class CancellationTokenExtensions
{
    public static Task AsTask(this System.Threading.CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>();
        token.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }
}
