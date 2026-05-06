using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public sealed class AffinityEntry
{
    public CharacterDefinition from;
    public CharacterDefinition to;
    public int                 points;

    [Tooltip("Track de relación. Ej: 'default', 'romance'. Vacío usa el defaultTrackId del schema.")]
    public string trackId = "default";

    public bool Matches(CharacterDefinition a, CharacterDefinition b) => from == a && to == b;
}

/// <summary>
/// Datos de diseño: estado inicial de las relaciones y configuración de simetría.
/// Solo lectura en runtime — el estado mutable vive en AffinityMapService.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Characters/Character Affinity Map", fileName = "CharacterAffinityMap")]
public sealed class CharacterAffinityMap : ScriptableObject
{
    [Header("Config")]
    public AffinitySchema schema;

    [Header("Relaciones iniciales")]
    [SerializeField] private List<AffinityEntry> entries = new();

    public IReadOnlyList<AffinityEntry> InitialEntries => entries;

    // --- Helpers para editores (solo lectura sobre datos de diseño) ---

    public IEnumerable<(CharacterDefinition other, int points, AffinityBand level, string trackId)>
        GetAllFor(CharacterDefinition origin)
    {
        if (!schema) yield break;
        foreach (var e in entries.Where(e => e.from == origin && e.to))
            yield return (e.to, e.points, schema.GetBandForPoints(e.points, e.trackId), e.trackId);
    }

    public IEnumerable<(CharacterDefinition other, int points, AffinityBand level, string trackId)>
        GetAllTowards(CharacterDefinition target)
    {
        if (!schema) yield break;
        foreach (var e in entries.Where(e => e.to == target && e.from))
            yield return (e.from, e.points, schema.GetBandForPoints(e.points, e.trackId), e.trackId);
    }
}
