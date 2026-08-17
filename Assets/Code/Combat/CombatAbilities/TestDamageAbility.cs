using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability/Test")]
public class TestDamageAbility : Ability
{
    public float m_Power;

    public override void Execute(CombatCharacter _Caster, List<CombatCharacter> _Targets)
    {
        foreach (var target in _Targets)
        {
            float damage = m_Power + _Caster.GetStats().m_Fuerza.m_Value - target.GetStats().m_Resistencia.m_Value;
            target.TakeDamage(damage);
        }
    }
}
