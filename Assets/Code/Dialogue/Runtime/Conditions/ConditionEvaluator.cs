using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConditionEvaluator : MonoBehaviour, IConditionEvaluator
{
    private readonly List<IChoiceCondition> _conditions = new()
    {
        new AffinityCondition(),
        new ProgressCondition()
    };

    public bool IsAllowed(ChoiceModule.ChoiceData c)
        => _conditions.All(cond => cond.IsMet(c));
}

public sealed class AffinityCondition : IChoiceCondition
{
    public bool IsMet(ChoiceModule.ChoiceData c)
    {
        if (c == null || !c.requiresAffinity) return true;
        var svc = AffinityServiceBootstrapper.Service;
        if (svc == null) return true;

        int     current = svc.GetPoints(c.affinityFrom, c.affinityTo);
        string  trackId = svc.GetTrack(c.affinityFrom, c.affinityTo);
        var     relationship = svc.Schema.GetTrack(trackId)?.Relationships
                            .FirstOrDefault(b => b.name == c.requiredAffinityRelationship);

        if (relationship == null) return true;
        return c.invertRequirement ? current < relationship.minInclusive : current >= relationship.minInclusive;
    }
}

public sealed class ProgressCondition : IChoiceCondition
{
    public bool IsMet(ChoiceModule.ChoiceData c)
    {
        if (c == null || !c.requiresProgress) return true;
        if (string.IsNullOrWhiteSpace(c.progressMethod)) return false;

        object arg = c.progressArgType switch
        {
            ProgressArgType.Int    => (object)c.progressArgInt,
            ProgressArgType.Float  => c.progressArgFloat,
            ProgressArgType.String => c.progressArgString ?? string.Empty,
            _                      => null
        };

        return ProgressConditionInvoker.TryInvoke(c.progressMethod, arg, out bool result) && result;
    }
}
