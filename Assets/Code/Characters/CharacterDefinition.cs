using System;
using UnityEngine;

public enum Sex { Unknown, Male, Female, Other }
public enum RaceKind { Beast, Parasite, Undead, Ethereal, Infernal, Deity, Machine, Curse, Human }

[CreateAssetMenu(menuName = "Dialogue/Characters/Character Definition", fileName = "NewCharacter")]
public class CharacterDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string characterId;

    [Header("Identity")]
    public string displayName;
    public Sex sex = Sex.Unknown;
    public RaceKind race = RaceKind.Human;
    [Min(0)] public int age;

    public string CharacterId => characterId;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(characterId))
            characterId = Guid.NewGuid().ToString("N");
    }
}

