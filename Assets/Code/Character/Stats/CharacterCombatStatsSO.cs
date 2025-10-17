using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatsSO", menuName = "Scriptable Objects/CharacterStatsSO")]
public class CharacterCombatStatsSO : ScriptableObject
{
    [Header("Character")]
    [SerializeField, Tooltip("Personaje origen")] CharacterProfile m_CharacterProfile;

    [Header("Estadisticas base nivel 1")]
    [SerializeField, Tooltip("Vida base del personaje")] float m_Vitalidad;
    [SerializeField, Tooltip("Ataque base del personaje")] float m_Fuerza;
    [SerializeField, Tooltip("Defensa base del personaje")] float m_Resistencia;
    [SerializeField, Tooltip("Afecta al orden de turno")] float m_Velocidad;
    [SerializeField, Tooltip("Afecta al % de hacer crítico")] float m_Suerte;

    [Header("Estadisticas por nivel")]
    [SerializeField, Tooltip("Vida por nivel del personaje")] float m_VitalidadPorNivel;
    [SerializeField, Tooltip("Ataque por nivel del personaje")] float m_FuerzaPorNivel;
    [SerializeField, Tooltip("Defensa por nivel del personaje")] float m_ResistenciaPorNivel;
    [SerializeField, Tooltip("Afecta al orden de turno")] float m_VelocidadPorNivel;
    [SerializeField, Tooltip("Afecta al % de hacer crítico")] float m_SuertePorNivel;

    [Header("Habilidades de combate")]
    [SerializeField] _CombatAbility[] m_CombatAbilities;
}
