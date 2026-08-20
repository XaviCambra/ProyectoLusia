using UnityEngine;

public class CombatCharacter : MonoBehaviour
{
    int m_Team = -1;
    [SerializeField] Character m_Character;
    [SerializeField] CombatCharacterStats m_Stats = new CombatCharacterStats();
    [SerializeField] HealthSystem m_HealthSystem = new HealthSystem();

    public float MaxHP => m_Stats.m_Vitalidad.m_Value;

    public void SetCharacter(Character _Character)
    {
        m_Character = _Character;
        m_Stats.SetStats(m_Character.GetStats());

        m_HealthSystem.GetComponent<HealthSystem>();
        m_HealthSystem.SetNewHealth(m_Stats.m_Vitalidad.m_Value);
    }

    public Character GetCharacter() => m_Character;
    public CombatCharacterStats GetStats() => m_Stats;

    public void TakeDamage(float _Amount)
    {
        m_HealthSystem.TakeDamage(_Amount);  
    }

    public void Heal(float _Amount)
    {
        m_HealthSystem.Heal(_Amount);
    }
}
