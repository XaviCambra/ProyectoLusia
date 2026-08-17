using UnityEngine;

public class CombatCharacter : MonoBehaviour
{
    int m_Team = -1;
    [SerializeField] Character m_Character;
    [SerializeField] CombatCharacterStats m_Stats = new CombatCharacterStats();

    private float m_CurrentHP;

    public float CurrentHP => m_CurrentHP;
    public float MaxHP => m_Stats.m_Vitalidad.m_Value;

    public void SetCharacter(Character _Character)
    {
        m_Character = _Character;
        m_Stats.SetStats(m_Character.GetStats());
        m_CurrentHP = m_Stats.m_Vitalidad.m_Value;
    }

    public Character GetCharacter() => m_Character;
    public CombatCharacterStats GetStats() => m_Stats;

    public void TakeDamage(float _Amount)
    {
        m_CurrentHP = Mathf.Max(0, m_CurrentHP - _Amount);  
    }

    public void Heal(float _Amount)
    {
        m_CurrentHP = Mathf.Min(MaxHP, m_CurrentHP + _Amount);
    }
}
