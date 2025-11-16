using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    // Lo meto todo aqui mientras me aclaro como hacerlo bien, demomento va an a ser tremendos espaguetis com tomatico code
    private void Update()
    {
        PlayTurn();
    }

    #region SISTEMA POR TURNOS
    public enum TurnPhase
    {
        ChooseAction,
        ChooseTarget,
        Resolve
    }

    int _currentActorIndex = 0; // si tienes varios combatientes
    TurnPhase _phase = TurnPhase.ChooseAction;

    _CombatAbility _ability;
    readonly List<CombatCharacterStatsSO> _targets = new();

    void PlayTurn()
    {
        switch (_phase)
        {
            case TurnPhase.ChooseAction:
                if (_ability != null)
                    Advance();
                break;

            case TurnPhase.ChooseTarget:
                if (_targets.Count > 0)
                    Advance();
                break;

            case TurnPhase.Resolve:
                ResolveAction();
                EndTurn();
                break;
        }
    }

    void Advance()
    {
        _phase = _phase switch
        {
            TurnPhase.ChooseAction => TurnPhase.ChooseTarget,
            TurnPhase.ChooseTarget => TurnPhase.Resolve,
            _ => TurnPhase.ChooseAction
        };
    }

    public void SetChosenAbility(_CombatAbility ability)
    {
        _ability = ability;
    }

    public void SetTargets(IEnumerable<CombatCharacterStatsSO> targets)
    {
        _targets.Clear();
        _targets.AddRange(targets);
    }

    void ResolveAction()
    {
        _ability.Apply(_targets);
    }

    void EndTurn()
    {
        _ability = null;
        _targets.Clear();
        _phase = TurnPhase.ChooseAction;

        // _currentActorIndex = GetNextActorIndex();
    }
    #endregion
}
