using UnityEngine;

public class CombatTurnManager : MonoBehaviour
{
    private enum ETurnState
    {
        PickAction,
        PickTarget,
        Execute
    }

    private ETurnState TurnState = ETurnState.PickAction;

    private void Update()
    {
        switch (TurnState)
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
                TurnState = ETurnState.PickAction;
                break;
        }
    }

    private void PickActionState()
    {
        TurnState = ETurnState.PickTarget;
    }

    private void PickTargetState()
    {
        TurnState = ETurnState.Execute;
    }

    private void Execute()
    {
        TurnState = ETurnState.PickAction;
    }
}
