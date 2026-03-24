using System;
using System.Collections.Generic;
using System.Linq;

public class CombatOrderManager
{
    private List<CharacterTurn> turnOrder = new List<CharacterTurn>();

    public IReadOnlyList<CharacterTurn> TurnOrder => turnOrder;

    public void AddCharacter(Character character)
    {
        var ct = new CharacterTurn
        {
            m_Character = character,
            m_TurnID = character.GetStats().Item4
        };

        turnOrder.Add(ct);
    }

    public void RemoveCharacter(Character character)
    {
        var ct = turnOrder.FirstOrDefault(t => t.m_Character == character);
        if (ct != null)
            turnOrder.Remove(ct);
    }

    public void SortByTurnID()
    {
        turnOrder = turnOrder
            .OrderBy(t => t.m_TurnID)
            .ToList();
    }

    public Character GetActiveCharacter()
    {
        return turnOrder.Count > 0 ? turnOrder[0].m_Character : null;
    }

    public void AdvanceTurn(float turnDelay)
    {
        if (turnOrder.Count == 0)
            return;

        turnOrder[0].m_TurnID += turnDelay;
        SortByTurnID();
    }
}

[Serializable]
public class CharacterTurn
{
    public Character m_Character;
    public float m_TurnID;
}
