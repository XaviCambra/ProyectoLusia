using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Character", order = 1)]
public class Character : ScriptableObject
{
    public Sprite sprite;

    #region Statistics
    [Header("Statistics")]
    [SerializeField] float m_Vitalidad;
    [SerializeField] float m_Fuerza;
    [SerializeField] float m_Resistencia;
    [SerializeField] float m_Velocidad;
    [SerializeField] float m_Suerte;
    #endregion

    public (float, float, float, float, float) GetStats()
    {
        return (m_Vitalidad, m_Fuerza, m_Resistencia, m_Velocidad, m_Suerte);
    }

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