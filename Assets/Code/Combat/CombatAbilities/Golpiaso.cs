using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Golpiaso", menuName = "Scriptable Objects/New Ability")]
public class Golpiaso : _CombatAbility
{
    [Header("Estadisticas")]
    public float m_Damage = 0;

    public override void Apply(List<CombatCharacterStatsSO> target)
    {
        base.Apply(target);

    }
}