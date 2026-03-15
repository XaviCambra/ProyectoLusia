using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CombatOrderManager : MonoBehaviour
{
    [SerializeField]
    List<CharacterTurn> m_CharacterOrder = new List<CharacterTurn>();

    CombatTurnManager m_CombatTurnManager = new CombatTurnManager();

    public void AddCharacter(Character _Character)
    {
        CharacterTurn l_Character = new CharacterTurn();
        l_Character.m_Character = _Character;
        l_Character.m_TurnID = _Character.GetStats().Item4;
        m_CharacterOrder.Add(l_Character);
        SortListBySpeed();
    }

    public void RemoveCharacter(Character _Character)
    {
        CharacterTurn l_CharacterToRemove = null;
        foreach (CharacterTurn _CharacterTurn in m_CharacterOrder)
        {
            if(_CharacterTurn.m_Character == _Character)
            {
                l_CharacterToRemove = _CharacterTurn;
            }
        }
        m_CharacterOrder.Remove(l_CharacterToRemove);
    }

    public void SortListBySpeed()
    {
        List<CharacterTurn> l_SortedList = m_CharacterOrder
            .OrderBy(x => x.m_TurnID)
            .ToList();
        m_CharacterOrder = l_SortedList;
    }

    public void SetActiveCharacter(int _TurnOrder = 0)
    {
        m_CombatTurnManager.SetActiveCharacter(m_CharacterOrder[_TurnOrder].m_Character);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_CombatTurnManager.PlayTurn())
            EndTurn(5); //EndTurn(m_CharacterOrder[0].m_TurnID);
    }



    public void EndTurn(float _TurnDelay)
    {
        m_CharacterOrder[0].m_TurnID += _TurnDelay;
        SortListBySpeed();
        SetActiveCharacter();
    }
}

[Serializable]
class CharacterTurn
{
    public Character m_Character;
    public float m_TurnID;
}