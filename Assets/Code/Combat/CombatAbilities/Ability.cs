using System.Collections.Generic;
using UnityEngine;

public abstract class Ability : ScriptableObject
{
    public string m_AbilityName;
    public AbilityTarget m_TargetType;

    public abstract void Execute(CombatCharacter _Caster, List<CombatCharacter> _Targets);
}