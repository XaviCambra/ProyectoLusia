using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class AffinityBand
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

    [SerializeField] private List<AffinityBand> bands = new();

    private List<AffinityBand> _sortedCache;
    private List<AffinityBand> SortedBands => _sortedCache ??= bands.OrderBy(b => b.ordinal).ToList();

    public IReadOnlyList<AffinityBand> Bands => SortedBands;
    public void InvalidateCache() => _sortedCache = null;

    public AffinityBand GetBandForPoints(int points)
    {
        var sorted = SortedBands;
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
        var sorted = SortedBands;
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
    [Header("Límites globales de puntos")]
    public int globalMin    = -100;
    public int globalMax    =  100;

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

    /// <summary>Devuelve el AffinityBand para los puntos dados en el track indicado (o el primero).</summary>
    public AffinityBand GetBandForPoints(int points, string trackId = null)
        => GetTrack(trackId)?.GetBandForPoints(points);

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
