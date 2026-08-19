using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class AffinityRelationship
{
    [Tooltip("Nombre visible. Ej: Rechazo / Vigilante / Cómplice / Con confianza / Vínculo emocional")]
    public string name;

    [Tooltip("Clave de localización para traducción futura. Si está vacía se usa 'name'.")]
    public string localizationKey;

    [Tooltip("Orden lógico ascendente. Los niveles se ordenan por este valor.")]
    public int ordinal;

    [Tooltip("Color identificativo del nivel en el editor.")]
    public Color color = Color.white;

    [Tooltip("Puntos mínimos (INCLUIDO) para este nivel.")]
    public int minInclusive;

    [Tooltip("Puntos máximos (EXCLUIDO). Usa int.MaxValue para sin tope.")]
    public int maxExclusive = int.MaxValue;

    public bool   Contains(int points) => points >= minInclusive && points < maxExclusive;
    public string DisplayName          => string.IsNullOrEmpty(localizationKey) ? name : localizationKey;
    public override string ToString()  => $"{name} [{minInclusive}, {maxExclusive}) (#{ordinal})";
}

/// <summary>
/// Una línea de progreso de afinidad con sus propios niveles (bands).
/// Ej: "amistad" tiene Familiar como tope; "romance" tiene Amor.
/// </summary>
[Serializable]
public sealed class AffinityTrack
{
    [Tooltip("Identificador único. Ej: 'default', 'romance', 'rivalidad'.")]
    public string id = "default";

    [Tooltip("Nombre legible en el editor.")]
    public string displayName = "Default";

    [SerializeField] private List<AffinityRelationship> relationships = new();

    private List<AffinityRelationship> _sortedCache;
    private List<AffinityRelationship> SortedRelationships => _sortedCache ??= relationships.OrderBy(b => b.ordinal).ToList();

    public IReadOnlyList<AffinityRelationship> Relationships => SortedRelationships;
    public void InvalidateCache() => _sortedCache = null;

    /// <summary>Minimo de puntos usable en este track: el minInclusive mas bajo de sus niveles.</summary>
    public int MinPoints
    {
        get
        {
            var sorted = SortedRelationships;
            return sorted.Count == 0 ? 0 : sorted.Min(r => r.minInclusive);
        }
    }

    /// <summary>
    /// Maximo de puntos usable en este track: el maxExclusive mas alto de sus niveles.
    /// Si el nivel superior no tiene tope (maxExclusive == int.MaxValue), se usa su propio
    /// minInclusive como techo real, ya que un rango sin fin no se puede mostrar en una barra/slider.
    /// </summary>
    public int MaxPoints
    {
        get
        {
            var sorted = SortedRelationships;
            if (sorted.Count == 0) return 0;

            int max = int.MinValue;
            foreach (var r in sorted)
            {
                int effective = r.maxExclusive == int.MaxValue ? r.minInclusive : r.maxExclusive;
                if (effective > max) max = effective;
            }
            return max;
        }
    }

    public AffinityRelationship GetRelationshipForPoints(int points)
    {
        var sorted = SortedRelationships;
        for (int i = 0; i < sorted.Count; i++)
            if (sorted[i].Contains(points)) return sorted[i];
        return null;

    }

    /// <summary>
    /// Color interpolado entre bands vecinas según la posición del punto dentro de su rango.
    /// El color puro de cada band coincide con el punto central de su rango.
    /// </summary>
    public Color GetColorForPoints(int points)
    {
        var sorted = SortedRelationships;
        if (sorted.Count == 0) return Color.gray;
        if (sorted.Count == 1) return sorted[0].color;

        int bandIdx = -1;
        for (int i = 0; i < sorted.Count; i++)
            if (sorted[i].Contains(points)) { bandIdx = i; break; }

        if (bandIdx < 0) return sorted[points < sorted[0].minInclusive ? 0 : sorted.Count - 1].color;

        var   band   = sorted[bandIdx];
        float center = (band.minInclusive + band.maxExclusive) * 0.5f;

        if (points < center && bandIdx > 0)
        {
            var   prev       = sorted[bandIdx - 1];
            float prevCenter = (prev.minInclusive + prev.maxExclusive) * 0.5f;
            float t          = Mathf.InverseLerp(prevCenter, center, points);
            return Color.Lerp(prev.color, band.color, t);
        }

        if (points >= center && bandIdx < sorted.Count - 1)
        {
            var   next       = sorted[bandIdx + 1];
            float nextCenter = (next.minInclusive + next.maxExclusive) * 0.5f;
            float t          = Mathf.InverseLerp(center, nextCenter, points);
            return Color.Lerp(band.color, next.color, t);
        }

        return band.color;
    }
}

[CreateAssetMenu(menuName = "Characters/Affinity/Affinity Schema", fileName = "AffinitySchema")]
public sealed class AffinitySchema : ScriptableObject
{
    [SerializeField] private List<AffinityTrack> tracks = new();

    public IReadOnlyList<AffinityTrack> Tracks => tracks;

    /// <summary>Devuelve el track con el id indicado, o el primero disponible.</summary>
    public AffinityTrack GetTrack(string trackId)
    {
        if (!string.IsNullOrEmpty(trackId))
        {
            var t = tracks.Find(x => x.id == trackId);
            if (t != null) return t;
        }
        return tracks.FirstOrDefault();
    }

    /// <summary>Devuelve el AffinityRelationship para los puntos dados en el track indicado (o el primero).</summary>
    public AffinityRelationship GetRelationshipForPoints(int points, string trackId = null)
        => GetTrack(trackId)?.GetRelationshipForPoints(points);

    /// <summary>Color interpolado entre bands vecinas para los puntos dados en el track indicado.</summary>
    public Color GetColorForPoints(int points, string trackId = null)
        => GetTrack(trackId)?.GetColorForPoints(points) ?? Color.gray;

    private void OnEnable()   => InvalidateAllCaches();
    private void OnValidate() => InvalidateAllCaches();

    private void InvalidateAllCaches()
    {
        foreach (var t in tracks)
            t?.InvalidateCache();
    }
}
