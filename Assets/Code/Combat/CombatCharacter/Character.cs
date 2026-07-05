using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Character", order = 1)]
public class Character : ScriptableObject
{
    public Sprite sprite;

    public CharacterStats m_CharacterStats;

    public CharacterStats GetStats() => m_CharacterStats;

    public List<Habilidades> m_Habilidades = new List<Habilidades>();

    #region States & Effects
    private HashSet<TileEffect> immunities = new();

    public void AddImmunity(TileEffect effect) => immunities.Add(effect);
    public void RemoveImmunity(TileEffect effect) => immunities.Remove(effect);

    public bool IsImmuneTo(TileEffect effect) => immunities.Contains(effect);
    #endregion
}

[Serializable]
public class Habilidades
{
    public string m_AbilityName;
    public enum AbilityTarget
    {
        SINGLE,
        MULTIPLE
    }
    public AbilityTarget m_Target;
}

[Serializable]
public struct CharacterStats
{
    public float m_Vitalidad;
    public float m_Fuerza;
    public float m_Resistencia;
    public float m_Velocidad;
    public float m_Suerte;
}
