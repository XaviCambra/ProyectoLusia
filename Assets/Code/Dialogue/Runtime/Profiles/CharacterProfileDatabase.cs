// Runtime/Profiles/CharacterProfileDatabase.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Profiles/Character Profile Database", fileName = "CharacterProfiles")]
public class CharacterProfileDatabase : ScriptableObject
{
    public List<CharacterProfile> profiles = new();
}
