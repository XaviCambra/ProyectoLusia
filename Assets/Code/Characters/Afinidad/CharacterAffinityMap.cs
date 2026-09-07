using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class AffinityEntry
{
    public CharacterDefinition from;
    public CharacterDefinition to;
    public int                 points;

    [Tooltip("Track de relación. Ej: 'default', 'romance'. Vacío usa el defaultTrackId del schema.")]
    public string trackId = "default";

    [Tooltip("Si 'from' conoce esta relacion todavia. Por defecto false: los datos pueden venir todos precableados desde el principio (mas facil de mantener interconectado) sin que eso signifique que el jugador ya se ha encontrado con ese personaje. Activar con IAffinityService.SetKnown cuando corresponda en la historia.")]
    public bool known;
}

/// <summary>
/// Datos de diseño: estado inicial de las relaciones y configuración de simetría.
/// Solo lectura en runtime — el estado mutable vive en AffinityMapService.
/// </summary>
[CreateAssetMenu(menuName = "Characters/Affinity/Character Affinity Map", fileName = "CharacterAffinityMap")]
public sealed class CharacterAffinityMap : ScriptableObject
{
    [Header("Config")]
    public AffinitySchema schema;

    [Header("Relaciones iniciales")]
    [SerializeField] private List<AffinityEntry> entries = new();

    public IReadOnlyList<AffinityEntry> InitialEntries => entries;
}
