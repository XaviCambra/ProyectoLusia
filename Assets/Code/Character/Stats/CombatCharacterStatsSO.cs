using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatsSO", menuName = "Scriptable Objects/CharacterStatsSO")]
public class CombatCharacterStatsSO : ScriptableObject
{
    [SerializeField, HideInInspector] private string characterId;
    [Header("Info")]
    [SerializeField, Tooltip("Nombre del personaje")] public string m_DisplayName;

    [Header("Estadisticas base")]
    [SerializeField, Tooltip("Vida base del personaje")] public float m_BaseVitalidad;
    [SerializeField, Tooltip("Ataque base del personaje")] public float m_BaseFuerza;
    [SerializeField, Tooltip("Defensa base del personaje")] public float m_BaseResistencia;
    [SerializeField, Tooltip("Afecta al orden de turno")] public float m_BaseVelocidad;
    [SerializeField, Tooltip("Afecta al % de hacer crítico")] public float m_BaseSuerte;

    [Header("Habilidades de combate")]
    public List<_CombatAbility> m_CombatAbilities = new List<_CombatAbility>();
}
