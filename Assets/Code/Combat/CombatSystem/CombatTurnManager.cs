using UnityEngine;

public class CombatTurnManager
{
    public enum ETurnState
    {
        PickAction,
        PickTarget,
        Execute
    }

    private ETurnState m_TurnState = ETurnState.PickAction;
    public ETurnState CurrentState => m_TurnState;

    public bool TurnFinished { get; private set; }

    public void SetActiveCharacter(CombatCharacter _Character)
    {
        m_TurnState = ETurnState.PickAction;
        TurnFinished = false;
    }

    public void Tick()
    {
        TurnFinished = false;

        switch (m_TurnState)
        {
            case ETurnState.PickAction:
                HandlePickAction();
                break;
            case ETurnState.PickTarget:
                HandlePickTarget();
                break;
            case ETurnState.Execute:
                HandleExecute();
                break;
        }
    }

    private void HandlePickAction()
    {
        if (Input.GetKeyUp(KeyCode.E))
            m_TurnState = ETurnState.PickTarget;
    }

    private void HandlePickTarget()
    {
        if (Input.GetKeyUp(KeyCode.E))
            m_TurnState = ETurnState.Execute;
    }

    private void HandleExecute()
    {
        TurnFinished = true;
    }
}
