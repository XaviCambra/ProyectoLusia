using UnityEngine;

public class CombatTurnManager : MonoBehaviour
{
    private enum ETurnState
    {
        PickAction,
        PickTarget,
        Execute
    }

    private ETurnState m_TurnState = ETurnState.PickAction;

    public void PlayTurn()
    {
        switch (m_TurnState)
        {
            case ETurnState.PickAction:
                PickActionState();
                break;
            case ETurnState.PickTarget:
                PickTargetState();
                break;
            case ETurnState.Execute:
                Execute();
                break;
            default:
                m_TurnState = ETurnState.PickAction;
                break;
        }
    }

    private void PickActionState()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            m_TurnState = ETurnState.PickTarget;
        }
    }

    private void PickTargetState()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            m_TurnState = ETurnState.Execute;
        }
    }

    private void Execute()
    {
        m_TurnState = ETurnState.PickAction;
    }
}
