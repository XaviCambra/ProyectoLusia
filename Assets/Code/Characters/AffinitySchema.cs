using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public sealed class AffinityBand
{
    [Tooltip("Nombre visible p.ej.: Rechazo / Vigilante / Tolerante / Cómplice / Con confianza / Vínculo emocional")]
    public string name;

    [Tooltip("Orden lógico (negativos menores, positivos mayores). Sirve para ordenar niveles.")]
    public int ordinal;

    [Tooltip("Puntos mínimos (INCLUIDO) para este nivel.")]
    public int minInclusive;

    [Tooltip("Puntos máximos (EXCLUIDO) para este nivel. Usa int.MaxValue para 'sin tope'.")]
    public int maxExclusive = int.MaxValue;

    public bool Contains(int points) => points >= minInclusive && points < maxExclusive;

    public override string ToString() => $"{name} [{minInclusive}, {maxExclusive}) (#{ordinal})";
}

[CreateAssetMenu(menuName = "Dialogue/Characters/Affinity Schema", fileName = "AffinitySchema")]
public class AffinitySchema : ScriptableObject
{
    [SerializeField] private List<AffinityBand> bands = new();

    public IReadOnlyList<AffinityBand> Bands => bands.OrderBy(b => b.ordinal).ToList();

    public AffinityBand GetBandForPoints(int points)
    {
        foreach (var b in Bands)
            if (b.Contains(points)) return b;
        return null; // si no cuadra, revisa solapamientos
    }

    private void OnValidate()
    {
        // Validación simple: no solapar y min < max
        foreach (var b in bands)
            if (b.minInclusive >= b.maxExclusive)
                Debug.LogError($"AffinitySchema '{name}': Nivel '{b.name}' tiene un rango inválido.");

        var ordered = Bands;
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var a = ordered[i];
            var c = ordered[i + 1];
            if (a.maxExclusive > c.minInclusive)
                Debug.LogWarning($"AffinitySchema '{name}': Posible solapamiento entre '{a.name}' y '{c.name}'.");
        }
    }
}

