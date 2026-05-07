using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConditionEvaluator : MonoBehaviour, IConditionEvaluator
{
    private readonly IChoiceCondition _affinity = new AffinityCondition();
    private readonly IChoiceCondition _progress  = new ProgressCondition();

    public bool IsAllowed(ChoiceModule.ChoiceData c)
        => _affinity.IsMet(c) && _progress.IsMet(c);
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
        var     band    = svc.Schema.GetTrack(trackId)?.Bands
                            .FirstOrDefault(b => b.name == c.requiredAffinityBand);

        if (band == null) return true;
        return c.invertRequirement ? current < band.minInclusive : current >= band.minInclusive;
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
