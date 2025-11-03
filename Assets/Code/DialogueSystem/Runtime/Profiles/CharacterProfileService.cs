// Runtime/Profiles/CharacterProfileService.cs
using System.Linq;
using UnityEngine;

public class CharacterProfileService : MonoBehaviour, ICharacterProfileService
{
    [Tooltip("Base de datos con todos los perfiles de personaje. Si no se asigna, se intentará resolver automáticamente.")]
    public CharacterProfileDatabase database;

    private CharacterProfileDatabase _db;                 // instancia efectiva en runtime
    private readonly System.Collections.Generic.Dictionary<string, CharacterProfile> _byId
        = new System.Collections.Generic.Dictionary<string, CharacterProfile>(128);


    private void OnEnable()
    {
        ResolveDatabase();
        RebuildIndex();
    }

    private void ResolveDatabase()
    {
        // 1) Inspector
        if (database != null)
        {
            _db = database;
            return;
        }

        // 2) Resources
        _db = Resources.Load<CharacterProfileDatabase>("Dialogue/CharacterProfiles");
        if (_db != null) return;

#if UNITY_EDITOR
        // 3) Fallback editor
        var guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterProfileDatabase");
        if (guids != null && guids.Length > 0)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            _db = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterProfileDatabase>(path);
            if (_db != null) return;
        }
#endif
    }

    private void RebuildIndex()
    {
        _byId.Clear();

        if (_db == null || _db.profiles == null || _db.profiles.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _db.profiles.Count; i++)
        {
            var p = _db.profiles[i];
            if (p == null) continue;

            var id = p.ProfileId;
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            _byId[id] = p;
        }
    }

    public CharacterProfile GetById(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return null;
        if (_db == null) return null;
        if (_byId.Count == 0) RebuildIndex();
        return _byId.TryGetValue(profileId, out var p) ? p : null;
    }
}
