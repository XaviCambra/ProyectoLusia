using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    // Lo meto todo aquí mientras me aclaro como hacerlo bien, demomento va an a ser tremendos espaguetis com tomatico code


    public void SetCharactersToCombat(List<CombatCharacter> l_CombatCharacter)
    {

    }


    private void Update()
    {
        PlayTurn();
    }


    #region SISTEMA POR TURNOS
    int m_TurnAction = 0;
    _CombatAbility m_CombatAbility = null;
    List<CharacterCombatStatsSO> m_Targets = new List<CharacterCombatStatsSO>();

    void PlayTurn()
    {
        switch(m_TurnAction)
        {
            case 0:
                ChooseAction();
                break;
            case 1:
                ChooseTarget();
                break;
            default:
                PlayActionOnTarget();
                break;
        }
    }

    void ChooseAction()
    {
        if (m_CombatAbility == null)
            return;

        m_TurnAction++;
    }

    void ChooseTarget()
    {
        if(m_Targets.Count == 0)
            return;

        m_TurnAction++;
    }

    void PlayActionOnTarget()
    {


        m_Targets.Clear();
        m_CombatAbility = null;
        m_TurnAction = 0;
    }
    #endregion
}
