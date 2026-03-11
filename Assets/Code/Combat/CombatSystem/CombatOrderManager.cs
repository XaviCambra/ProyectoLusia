using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatOrderManager : MonoBehaviour
{
    [SerializeField]
    List<CharacterTurn> m_CharacterOrder = new List<CharacterTurn>();
    private int m_CharacterTurn = 0;

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

    // Update is called once per frame
    void Update()
    {
        
    }


}

[Serializable]
class CharacterTurn
{
    public Character m_Character;
    public float m_TurnID;
}