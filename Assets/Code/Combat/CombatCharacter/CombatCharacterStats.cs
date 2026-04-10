using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class CombatCharacterStats
{
    public CombatStat m_Vitalidad;
    public CombatStat m_Fuerza;
    public CombatStat m_Resistencia;
    public CombatStat m_Velocidad;
    public CombatStat m_Suerte;

    public void SetStats(CharacterStats _CharacterStats)
    {
        m_Vitalidad.m_BaseValue     = _CharacterStats.m_Vitalidad;
        m_Fuerza.m_BaseValue        = _CharacterStats.m_Fuerza;
        m_Resistencia.m_BaseValue   = _CharacterStats.m_Resistencia;
        m_Velocidad.m_BaseValue     = _CharacterStats.m_Velocidad;
        m_Suerte.m_BaseValue        = _CharacterStats.m_Suerte;
    }
}

[Serializable]
public class CombatStat
{
    public float m_BaseValue;
    private List<float> m_StatModifiers = new();

    public float m_Value => m_BaseValue + m_StatModifiers.Sum();

    public void AddModifier(float _Modification) => m_StatModifiers.Add(_Modification);
    public void RemoveModifier(float _Modification) => m_StatModifiers.Remove(_Modification);
}