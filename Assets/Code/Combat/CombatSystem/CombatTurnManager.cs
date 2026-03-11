using UnityEngine;

public class CombatTurnManager
{
    private enum ETurnState
    {
        PickAction,
        PickTarget,
        Execute
    }

    private ETurnState m_TurnState = ETurnState.PickAction;

    private Character m_ActiveCharacter;

    public void SetActiveCharacter(Character _Character)
    {
        m_ActiveCharacter = _Character;
    }

    public bool PlayTurn()
    {
        switch (m_TurnState)
        {
            case ETurnState.PickAction:
                return PickActionState();
            case ETurnState.PickTarget:
                return PickTargetState();
            case ETurnState.Execute:
                return Execute();
            default:
                m_TurnState = ETurnState.PickAction;
                break;
        }
        return false;
    }

    private bool PickActionState()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            m_TurnState = ETurnState.PickTarget;
        }
        return false;
    }

    private bool PickTargetState()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            m_TurnState = ETurnState.Execute;
        }
        return false;
    }

    private bool Execute()
    {
        m_TurnState = ETurnState.PickAction;
        return true;
    }
}
