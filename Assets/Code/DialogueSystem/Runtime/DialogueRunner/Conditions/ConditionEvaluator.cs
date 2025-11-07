using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConditionEvaluator : MonoBehaviour, IConditionEvaluator
{
    private readonly IChoiceCondition _affinity = new AffinityCondition();
    private readonly IChoiceCondition _progress = new ProgressCondition();

    public bool IsAllowed(DialogueNodeData.ChoiceData c)
        => _affinity.IsMet(c) && _progress.IsMet(c);
}

public sealed class AffinityCondition : IChoiceCondition
{
    public bool IsMet(DialogueNodeData.ChoiceData c)
    {
        if (c == null || !c.requiresAffinity) return true;
        var key = c.affinityKey ?? string.Empty;
        float current = ParamService.GetFloat(key, 0f);
        return c.invertRequirement ? current < c.requiredAffinity : current >= c.requiredAffinity;
    }
}

public sealed class ProgressCondition : IChoiceCondition
{
    public bool IsMet(DialogueNodeData.ChoiceData c)
    {
        if (c == null || !c.requiresProgress) return true;
        if (string.IsNullOrWhiteSpace(c.progressMethod)) return false;

        object arg = c.progressArgType switch
        {
            ProgressArgType.Int => (object)c.progressArgInt,
            ProgressArgType.Float => c.progressArgFloat,
            ProgressArgType.String => c.progressArgString ?? string.Empty,
            _ => null
        };

        return ProgressConditionInvoker.TryInvoke(c.progressMethod, arg, out bool result) && result;
    }
}
