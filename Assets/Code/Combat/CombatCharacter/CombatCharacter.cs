using UnityEngine;
using UnityEngine.TextCore.Text;

public class CombatCharacter : MonoBehaviour
{
    Character m_Character;
    CombatCharacterStats m_Stats;

    public void SetCharacter(Character _Character)
    {
        m_Character = _Character;
        m_Stats.SetStats(m_Character.GetStats());
    }

    public CombatCharacterStats GetStats() => m_Stats;
}
