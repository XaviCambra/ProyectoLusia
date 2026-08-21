using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="ImageChoiceModule"/>.
/// Presenta las opciones como cuadrícula de sprites vía <see cref="IChatPresenter"/>.
/// Devuelve el puerto elegido como resultado de <see cref="ExecuteAsync"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatImageChoiceModuleExecutor : ModuleExecutorBase, IPresenterHost
{
    [SerializeField] private MonoBehaviour presenterRef; // IChatPresenter

    private IChatPresenter _presenter;

    public override Type ModuleType => typeof(ImageChoiceModule);

    private void Awake()
    {
        _presenter = presenterRef as IChatPresenter;
    }

    public void SetPresenter(IChatPresenter presenter) => _presenter = presenter;

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (ImageChoiceModule)module;

        var idx = _presenter != null
            ? await _presenter.ShowImageChoicesAsync(m.choices, ctx.CurrentProfile, ctx.Token)
            : 0;

        return m.choices.Count > idx ? m.choices[idx].portName : "Next";
    }
}
