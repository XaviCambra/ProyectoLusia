using System;
using System.Threading.Tasks;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. El homologo de Retratos es ChoiceModuleExecutor.
/// <summary>
/// Executor para <see cref="ChoiceModule"/> en el motor de Chat.
/// Presenta las opciones como burbuja inline vía <see cref="IChatPresenter"/>,
/// filtradas por <see cref="IConditionEvaluator"/> (afinidad/progreso) si hay uno
/// asignado. Devuelve el puerto elegido como resultado de <see cref="ExecuteAsync"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatChoiceModuleExecutor : ModuleExecutorBase, IPresenterHost
{
    [SerializeField] private MonoBehaviour presenterRef;          // IChatPresenter
    [SerializeField] private MonoBehaviour conditionEvaluatorRef; // IConditionEvaluator (opcional)

    private IChatPresenter      _presenter;
    private IConditionEvaluator _conditionEvaluator;

    public override Type ModuleType => typeof(ChoiceModule);

    private void Awake()
    {
        _presenter          = presenterRef as IChatPresenter;
        _conditionEvaluator = conditionEvaluatorRef as IConditionEvaluator;
    }

    public void SetPresenter(IChatPresenter presenter) => _presenter = presenter;

    public override async Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (ChoiceModule)module;

        var visible = _conditionEvaluator != null
            ? m.choices.FindAll(_conditionEvaluator.IsAllowed)
            : m.choices;

        var idx = _presenter != null
            ? await _presenter.ShowChoicesAsync(visible, ctx.CurrentProfile, ctx.Token)
            : 0;

        return visible.Count > idx ? visible[idx].portName : "Next";
    }
}
