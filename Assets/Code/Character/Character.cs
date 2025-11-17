using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Character : MonoBehaviour
{
    public CombatCharacterStatsSO m_CombatCharacterSO;

    #region Statistics
    [Header("Statistics")]
    float m_Vitalidad;
    float m_Fuerza;
    float m_Resistencia;
    float m_Velocidad;
    float m_Suerte;

    float m_Affinity;
    #endregion

    #region Getters i Setters
    void SetStats()
    {
        m_Vitalidad = m_CombatCharacterSO.m_BaseVitalidad;
        m_Fuerza = m_CombatCharacterSO.m_BaseFuerza;
        m_Resistencia = m_CombatCharacterSO.m_BaseResistencia;
        m_Velocidad = m_CombatCharacterSO.m_BaseVelocidad;
        m_Suerte = m_CombatCharacterSO.m_BaseSuerte;
    }

    List<_CombatAbility> GetAbilities()
    {
        return m_CombatCharacterSO.m_CombatAbilities;
    }
    #endregion

    #region States & Effects
    private HashSet<TileEffect> immunities = new();

    public void AddImmunity(TileEffect effect) => immunities.Add(effect);
    public void RemoveImmunity(TileEffect effect) => immunities.Remove(effect);

    public bool IsImmuneTo(TileEffect effect) => immunities.Contains(effect);
    #endregion



}
