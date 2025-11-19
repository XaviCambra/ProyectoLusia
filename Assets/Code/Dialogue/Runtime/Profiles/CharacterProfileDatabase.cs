// Runtime/Profiles/CharacterProfileDatabase.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Profiles/Character Profile Database", fileName = "CharacterProfiles")]
public class CharacterProfileDatabase : ScriptableObject
{
    public List<CharacterProfile> profiles = new();

    private Dictionary<string, CharacterProfile> _byId;

    private void OnEnable()
    {
        BuildIndex();
    }

    public void BuildIndex()
    {
        _byId = new Dictionary<string, CharacterProfile>();
        foreach (var p in profiles)
        {
            if (p == null) continue;
            var id = p.ProfileId;
            if (!string.IsNullOrEmpty(id) && !_byId.ContainsKey(id))
                _byId.Add(id, p);
        }
    }

    public CharacterProfile FindById(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return null;

        if (_byId == null)
            BuildIndex();

        _byId.TryGetValue(profileId, out var p);
        return p;
    }
}