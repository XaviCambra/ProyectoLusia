using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Character", order = 1)]
public class Character : ScriptableObject
{
    public Sprite sprite;

    public CharacterStats m_CharacterStats;

    #region States & Effects
    private HashSet<TileEffect> immunities = new();

    public void AddImmunity(TileEffect effect) => immunities.Add(effect);
    public void RemoveImmunity(TileEffect effect) => immunities.Remove(effect);

    public bool IsImmuneTo(TileEffect effect) => immunities.Contains(effect);
    #endregion

    public CharacterStats GetStats() => m_CharacterStats;

    public List<Ability> m_AbilityList = new List<Ability>();
}