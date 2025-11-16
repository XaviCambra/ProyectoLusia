using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Character : MonoBehaviour
{
    public CombatCharacterStatsSO m_CombatCharacterSO;

    public List<_CombatAbility> m_CombatAbilities = new List<_CombatAbility>();

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
    void GetStats()
    {
        m_Vitalidad = m_CombatCharacterSO.m_BaseVitalidad;
        m_Fuerza = m_CombatCharacterSO.m_BaseFuerza;
        m_Resistencia = m_CombatCharacterSO.m_BaseResistencia;
        m_Velocidad = m_CombatCharacterSO.m_BaseVelocidad;
        m_Suerte = m_CombatCharacterSO.m_BaseSuerte;
    }

    void GetAbilities()
    {
        foreach(_CombatAbility l_CombatAbility in m_CombatCharacterSO.m_CombatAbilities)
        {
            m_CombatAbilities.Add(l_CombatAbility);
        }
    }
    #endregion

    #region States & Effects
    private HashSet<TileEffect> immunities = new();

    public void AddImmunity(TileEffect effect) => immunities.Add(effect);
    public void RemoveImmunity(TileEffect effect) => immunities.Remove(effect);

    public bool IsImmuneTo(TileEffect effect) => immunities.Contains(effect);
    #endregion



}
