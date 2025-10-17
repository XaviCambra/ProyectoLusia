using UnityEngine;

//[CreateAssetMenu(fileName = "AbilityTest", menuName = "Scriptable Objects/AbilityTest")]
public class _CombatAbility : ScriptableObject
{
    [SerializeField] protected string m_NombreHabilidad;
    [SerializeField] protected ECombatTarget _target;

    protected virtual void ApplyEffect() { }
}
