using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private CombatCharacterManager l_CombatCharacterManager = new CombatCharacterManager();
    private CombatOrderManager l_OrderManager = new CombatOrderManager();
    private CombatTurnManager l_TurnManager = new CombatTurnManager();
    private CombatUIManager l_UiManager;

    private bool l_CombatStarted = false;
    private bool l_IsTransitioningTurn = false;


    private void Awake()
    {
        l_UiManager = GetComponent<CombatUIManager>();
    }

    public void StartCombat(List<Character> _Characters)
    {
        /*
         * ESTO ESTA BAJO TESTEO Y ES PROVISIONAL
         * 
         * Estan hardcodeadas las siguientes cosas
         *   1. El Trigger que inicia el combate.
         *      Script Name = TestCombatHardcode.cs
         *      Debería ser el sistema de exploración quien lance este trigger
         *   2. El sistema de equipos
         *      Script Name = Test_BaseTeam.cs
         *      He supuesto que sera un singleton que tendra info de quien esta en el equipo actualmente
         *      Nos puede facilitar el trabajo de cara a que todos los sitemas funcionen con ese singleton
         *      
         */
        Test_BaseTeam Team1 = FindFirstObjectByType<Test_BaseTeam>().gameObject.GetComponent<Test_BaseTeam>();
        foreach (var c in Team1.characterList)
        {
            l_CombatCharacterManager.AddCharacterToTeam(c, 1);
            //l_OrderManager.AddCharacter(l_CombatCharacter);
        }

        foreach (var c in _Characters)
        {
            //CombatCharacter l_CombatCharacter = new CombatCharacter();
            //l_CombatCharacter.SetCharacter(c);
            //l_CombatCharacterManager.AddCharacterToTeam(l_CombatCharacter, 2);
            //l_OrderManager.AddCharacter(l_CombatCharacter);
        }

        l_OrderManager.SortByTurnID();

        l_UiManager.BuildUI(l_OrderManager.TurnOrder);

        l_TurnManager.SetActiveCharacter(l_OrderManager.GetActiveCharacter());

        l_UiManager.OnCharacterClicked = OnCharacterClicked;

        l_CombatStarted = true;
    }

    private void Update()
    {
        if (!l_CombatStarted)
            return;

        if (l_IsTransitioningTurn)
            return;

        l_TurnManager.Tick();
        l_UiManager.UpdateUI(l_TurnManager.CurrentState);

        if (l_TurnManager.TurnFinished)
            EndTurn();
    }

    private void EndTurn()
    {
        l_IsTransitioningTurn = true;

        l_OrderManager.AdvanceTurn(5f);

        l_UiManager.AnimateReorder(l_OrderManager.TurnOrder, OnTurnReorderFinished);
    }

    private void OnTurnReorderFinished()
    {
        l_TurnManager.SetActiveCharacter(l_OrderManager.GetActiveCharacter());

        l_IsTransitioningTurn = false;
    }


    private void OnCharacterClicked(CombatCharacter clicked)
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
