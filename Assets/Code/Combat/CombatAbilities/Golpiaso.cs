using UnityEngine;

[CreateAssetMenu(fileName = "Golpiaso", menuName = "Scriptable Objects/New Ability")]
public class Golpiaso : _CombatAbility
{
    [SerializeField] protected ECombatTarget _target;

    [Header("Estadisticas")]
    public float m_Damage = 0;

    protected override void PlayAbility()
    {
        base.PlayAbility();

    }
}