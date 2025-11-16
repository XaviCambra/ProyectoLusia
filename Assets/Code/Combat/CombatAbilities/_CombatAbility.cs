using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "AbilityTest", menuName = "Scriptable Objects/AbilityTest")]
public class _CombatAbility : ScriptableObject
{
    [Header("UI")]
    [SerializeField] public string m_AbilityName;
    [SerializeField] public Sprite m_AbilityIcon;

    [Header("Targeting")]
    public ECombatTarget m_TargetTeam = ECombatTarget.Enemy;
    public ECombatScope m_TargetScope = ECombatScope.Single;
    [Tooltip("Se ignora cuando TargetScope es All")] public int m_TargetsCount = 1;

    public virtual void Apply(List<CombatCharacterStatsSO> target) { }
}


