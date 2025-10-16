// Runtime/Profiles/CharacterProfileService.cs
using UnityEngine;

public class CharacterProfileService : MonoBehaviour, ICharacterProfileService
{
    [Tooltip("Base de datos con todos los perfiles de personaje.")]
    public CharacterProfileDatabase database;

    public CharacterProfile GetById(string profileId)
    {
        return database ? database.FindById(profileId) : null;
    }
}
