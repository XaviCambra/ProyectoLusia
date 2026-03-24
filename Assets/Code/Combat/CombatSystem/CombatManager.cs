using System.Collections.Generic;
using UnityEngine;
using static CombatTurnManager;

public class CombatManager : MonoBehaviour
{
    private CombatOrderManager orderManager = new CombatOrderManager();
    private CombatTurnManager turnManager = new CombatTurnManager();
    private CombatUIManager uiManager;

    private bool combatStarted = false;
    private bool isTransitioningTurn = false;


    private void Awake()
    {
        uiManager = GetComponent<CombatUIManager>();
    }

    public void StartCombat(List<Character> characters)
    {
        foreach (var c in characters)
            orderManager.AddCharacter(c);

        orderManager.SortByTurnID();

        uiManager.BuildUI(orderManager.TurnOrder);

        turnManager.SetActiveCharacter(orderManager.GetActiveCharacter());

        uiManager.OnCharacterClicked = OnCharacterClicked;

        combatStarted = true;
    }

    private void Update()
    {
        if (!combatStarted)
            return;

        if (isTransitioningTurn)
            return;

        turnManager.Tick();
        uiManager.UpdateUI(turnManager.CurrentState);

        if (turnManager.TurnFinished)
            EndTurn();
    }

    private void EndTurn()
    {
        isTransitioningTurn = true;

        orderManager.AdvanceTurn(5f);

        uiManager.AnimateReorder(orderManager.TurnOrder, OnTurnReorderFinished);
    }

    private void OnTurnReorderFinished()
    {
        turnManager.SetActiveCharacter(orderManager.GetActiveCharacter());

        isTransitioningTurn = false;
    }


    private void OnCharacterClicked(Character clicked)
    {
        Debug.Log("Has hecho clic en " + clicked.name);

        // Aquí decides qué hacer:
        // - seleccionar objetivo
        // - abrir menú de habilidades
        // - mostrar stats
        // - etc.

        // CombatManager no ejecuta acciones aquí todavía,
        // solo recibe la notificación.
    }

}
