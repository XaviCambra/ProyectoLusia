using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AffinitySymmetry
{
    Directional, // A->B independiente de B->A
    Mirror       // Cuando se cambia A->B, refleja el mismo valor en B->A
}

[Serializable]
public sealed class AffinityEntry
{
    public CharacterDefinition from;
    public CharacterDefinition to;
    public int points;

    public bool Matches(CharacterDefinition a, CharacterDefinition b) => from == a && to == b;
}

[CreateAssetMenu(menuName = "Dialogue/Characters/Character Affinity Map", fileName = "CharacterAffinityMap")]
public class CharacterAffinityMap : ScriptableObject
{
    [Header("Config")]
    public AffinitySchema schema;
    public AffinitySymmetry symmetry = AffinitySymmetry.Directional;

    [Header("Data")]
    [SerializeField] private List<AffinityEntry> entries = new();

    public int GetPoints(CharacterDefinition from, CharacterDefinition to)
    {
        var e = entries.FirstOrDefault(x => x.Matches(from, to));
        return e?.points ?? 0;
    }

    public AffinityBand GetLevel(CharacterDefinition from, CharacterDefinition to)
    {
        if (!schema) return null;
        return schema.GetBandForPoints(GetPoints(from, to));
    }

    public void SetPoints(CharacterDefinition from, CharacterDefinition to, int newPoints)
    {
        if (!from || !to) return;

        var e = entries.FirstOrDefault(x => x.Matches(from, to));
        if (e == null)
        {
            e = new AffinityEntry { from = from, to = to, points = newPoints };
            entries.Add(e);
        }
        else
        {
            e.points = newPoints;
        }

        if (symmetry == AffinitySymmetry.Mirror)
        {
            var r = entries.FirstOrDefault(x => x.Matches(to, from));
            if (r == null)
            {
                r = new AffinityEntry { from = to, to = from, points = newPoints };
                entries.Add(r);
            }
            else
            {
                r.points = newPoints;
            }
        }
    }

    public int AddPoints(CharacterDefinition from, CharacterDefinition to, int delta)
    {
        int current = GetPoints(from, to);
        int next = current + delta;
        SetPoints(from, to, next);
        return next;
    }

    public IEnumerable<(CharacterDefinition other, int points, AffinityBand level)> GetAllFor(CharacterDefinition origin)
    {
        if (!schema) yield break;

        var q = entries.Where(e => e.from == origin);
        foreach (var e in q)
            yield return (e.to, e.points, schema.GetBandForPoints(e.points));
    }
}
