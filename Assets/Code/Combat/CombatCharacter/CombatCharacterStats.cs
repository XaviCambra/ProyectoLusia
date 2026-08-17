using System;

[Serializable]
public class CombatCharacterStats
{
    public CombatStat m_Vitalidad = new CombatStat();
    public CombatStat m_Fuerza = new CombatStat();
    public CombatStat m_Resistencia = new CombatStat();
    public CombatStat m_Velocidad = new CombatStat();
    public CombatStat m_Suerte = new CombatStat();

    public void SetStats(CharacterStats _CharacterStats)
    {
        m_Vitalidad.m_BaseValue     = _CharacterStats.m_Vitalidad;
        m_Fuerza.m_BaseValue        = _CharacterStats.m_Fuerza;
        m_Resistencia.m_BaseValue   = _CharacterStats.m_Resistencia;
        m_Velocidad.m_BaseValue     = _CharacterStats.m_Velocidad;
        m_Suerte.m_BaseValue        = _CharacterStats.m_Suerte;
    }
}

