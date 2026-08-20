using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatCharacterManager : MonoBehaviour
{
    [SerializeField] List<CombatCharacterTeam> m_CombatCharacterTeamList = new List<CombatCharacterTeam>();
    [SerializeField] List<CombatCharacter> m_TeamCharacterList = new List<CombatCharacter>();
    [SerializeField] List<CombatCharacter> m_EnemyCharacterList = new List<CombatCharacter>();
    public void AddCharacterToTeam(Character _CombatCharacter, int _Team = 0)
    {
        if (_CombatCharacter == null)
            return;



        //m_CombatCharacterTeamList.Add(new CombatCharacterTeam(_CombatCharacter, _Team));
    }

}

[Serializable]
public class CombatCharacterTeam
{
    public CombatCharacterTeam(CombatCharacter combatCharacter, int team)
    {
        m_CombatCharacter = combatCharacter;
        m_Team = team;
    }
    public CombatCharacter CombatCharacter => m_CombatCharacter;
    public int Team => m_Team;

    [SerializeField] private CombatCharacter m_CombatCharacter;
    [SerializeField] private int m_Team;
}