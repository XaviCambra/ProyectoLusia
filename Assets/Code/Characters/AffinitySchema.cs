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

    [Tooltip("Puntos máximos (EXCLUIDO) para este nivel. Usa int.MaxValue para sin tope.")]
    public int maxExclusive = int.MaxValue;

    public bool   Contains(int points) => points >= minInclusive && points < maxExclusive;
    public string DisplayName          => string.IsNullOrEmpty(localizationKey) ? name : localizationKey;

    public override string ToString() => $"{name} [{minInclusive}, {maxExclusive}) (#{ordinal})";
}

[CreateAssetMenu(menuName = "Dialogue/Characters/Affinity Schema", fileName = "AffinitySchema")]
public sealed class AffinitySchema : ScriptableObject
{
    [SerializeField] private List<AffinityBand> bands = new();

    [Header("Límites globales de puntos")]
    public int globalMin    = -100;
    public int globalMax    =  100;

    [Header("Puntos de una relación nueva sin datos guardados")]
    public int defaultPoints = 0;

    // Lista ordenada cacheada — se reconstruye solo cuando cambian los datos
    private List<AffinityBand> _sortedCache;
    private List<AffinityBand> SortedBands => _sortedCache ??= bands.OrderBy(b => b.ordinal).ToList();

    public IReadOnlyList<AffinityBand> Bands => SortedBands;

    public AffinityBand GetBandForPoints(int points)
    {
        var sorted = SortedBands;
        for (int i = 0; i < sorted.Count; i++)
            if (sorted[i].Contains(points)) return sorted[i];
        return null;
    }

    private void OnEnable()    => _sortedCache = null;
    private void OnValidate()
    {
        _sortedCache = null;

        foreach (var b in bands)
            if (b.minInclusive >= b.maxExclusive)
                Debug.LogError($"AffinitySchema '{name}': '{b.name}' tiene rango inválido.");

        var ordered = SortedBands;
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var a = ordered[i];
            var c = ordered[i + 1];
            if (a.maxExclusive > c.minInclusive)
                Debug.LogWarning($"AffinitySchema '{name}': solapamiento entre '{a.name}' y '{c.name}'.");
        }
    }
}
