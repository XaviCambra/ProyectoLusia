using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Character : MonoBehaviour
{
    #region Statistics
    [Header("Statistics")]
    float m_Vitalidad;
    float m_Fuerza;
    float m_Resistencia;
    float m_Velocidad;
    float m_Suerte;

    float m_Affinity;
    #endregion

    

    #region States & Effects
    private HashSet<TileEffect> immunities = new();

    public void AddImmunity(TileEffect effect) => immunities.Add(effect);
    public void RemoveImmunity(TileEffect effect) => immunities.Remove(effect);

    public bool IsImmuneTo(TileEffect effect) => immunities.Contains(effect);
    #endregion



}
