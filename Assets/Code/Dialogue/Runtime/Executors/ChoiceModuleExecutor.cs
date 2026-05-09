using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Executor para <see cref="ChoiceModule"/>.
/// Muestra las opciones de elección y espera a que el jugador seleccione una.
/// Implementa <see cref="IChoiceExecutor"/> para que el runner dependa
/// de la interfaz, no de esta clase concreta (DIP).
/// Las hotkeys numéricas (1-4) se gestionan en el propio Update,
/// eliminando la dependencia del runner en este tipo concreto.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChoiceModuleExecutor : ModuleExecutorBase, IChoiceExecutor
{
    [SerializeField] private MonoBehaviour choiceUIRef; // IChoiceUIController

    private IChoiceUIController _choiceUI;
    private TaskCompletionSource<bool> _waitTcs;

    /// <inheritdoc/>
    public event Action<string> OnChoiceSelected;

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
        _waitTcs?.TrySetResult(true);
        OnChoiceSelected?.Invoke(chosenPort);
    }

    public override void OnNodeBegin() => _choiceUI?.BeginNode();

    public override async Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        if (_choiceUI == null) return;

        var m = (ChoiceModule)module;
        _waitTcs = new TaskCompletionSource<bool>();

        bool shown = _choiceUI.Show(m, ctx.NodeGuid, port =>
        {
            _choiceUI.Hide();
            OnChoiceSelected?.Invoke(port);
            _waitTcs.TrySetResult(true);
        });

        if (!shown)
        {
            _waitTcs.TrySetResult(true);
            return;
        }

        await Task.WhenAny(_waitTcs.Task, ctx.Token.AsTask());
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
