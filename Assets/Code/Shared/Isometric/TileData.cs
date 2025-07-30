using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public Vector3Int CellPosition { get; }
    public ETileType BaseType { get; private set; }
    public HashSet<TileEffect> Effects { get; private set; } = new();

    // Ocupante actual (null si está libre)
    public GameObject Occupant { get; set; }

    public TileData(Vector3Int cellPosition, ETileType baseType)
    {
        CellPosition = cellPosition;
        BaseType = baseType;
    }

    /// <summary>
    /// Indica si la casilla es transitable (según el terreno y si está ocupada).
    /// </summary>
    public bool IsWalkable()
    {
        // Ejemplo: solo Free es transitable si no hay ocupante.
        return (BaseType == ETileType.Free) && Occupant == null;
    }

    /// <summary>
    /// Devuelve el coste de movimiento para esta casilla, considerando los efectos activos y el personaje que la atraviesa.
    /// El coste de cada efecto se consulta en SOTileEffectRules.
    /// </summary>
    /// <param name="character">El personaje que intenta moverse a esta casilla.</param>
    /// <param name="rules">Asset ScriptableObject con las reglas de costes de efectos.</param>
    /// <returns>Coste total de movimiento para esta casilla para ese personaje.</returns>
    public int GetMovementCost(CharacterStats character, SOTileEffectRules rules)
    {
        int cost = 1; // Coste base de moverse a una casilla “limpia”
        foreach (var effect in Effects)
        {
            if (!character.IsImmuneTo(effect))
            {
                cost += rules.GetCost(effect);
            }
        }
        // Puedes añadir lógica adicional según el tipo base o habilidades especiales
        return cost;
    }

    /// <summary>
    /// Añade un efecto si no está ya presente.
    /// </summary>
    public void AddEffect(TileEffect effect)
    {
        Effects.Add(effect);
    }

    /// <summary>
    /// Quita un efecto si está presente.
    /// </summary>
    public void RemoveEffect(TileEffect effect)
    {
        Effects.Remove(effect);
    }

    /// <summary>
    /// Comprueba si la casilla tiene un efecto concreto.
    /// </summary>
    public bool HasEffect(TileEffect effect)
    {
        return Effects.Contains(effect);
    }

    /// <summary>
    /// Elimina todos los efectos activos.
    /// </summary>
    public void ClearEffects()
    {
        Effects.Clear();
    }

    /// <summary>
    /// Comprueba si hay algún efecto activo.
    /// </summary>
    public bool HasAnyEffect()
    {
        return Effects.Count > 0;
    }
}
