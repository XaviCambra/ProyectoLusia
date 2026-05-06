using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Characters/Character Database", fileName = "CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private List<CharacterDefinition> characters = new();

    public IReadOnlyList<CharacterDefinition> All => characters;

    public CharacterDefinition GetById(string id) =>
        characters.FirstOrDefault(c => c && c.CharacterId == id);

    public CharacterDefinition GetByName(string name) =>
        characters.FirstOrDefault(c => c && c.displayName == name);
}
