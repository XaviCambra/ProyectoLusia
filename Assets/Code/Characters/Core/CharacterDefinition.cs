using System;
using System.Collections.Generic;
using UnityEngine;

public enum Sex { Unknown, Male, Female, Other }
public enum RaceKind { Beast, Parasite, Undead, Ethereal, Infernal, Deity, Machine, Curse, Human }

[CreateAssetMenu(menuName = "Dialogue/Characters/Character Definition", fileName = "NewCharacter")]
public class CharacterDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string characterId;

    [Header("Identity")]
    public string displayName;
    public Sprite icon;
    public Sex sex = Sex.Unknown;
    public RaceKind race = RaceKind.Human;
    [Min(0)] public int age;

    [Header("Chat")]
    public Sprite avatarSprite;

    [Header("Retratos")]
    public List<PortraitEntry> portraits = new();

    public string CharacterId => characterId;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(characterId))
            characterId = Guid.NewGuid().ToString("N");
    }

    public Sprite GetPortraitByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        var p = portraits.Find(x => x != null && x.key == key);
        return p != null ? p.sprite : null;
    }

    public IEnumerable<string> GetPortraitKeys()
    {
        foreach (var p in portraits)
            if (p != null && !string.IsNullOrEmpty(p.key))
                yield return p.key;
    }
}

