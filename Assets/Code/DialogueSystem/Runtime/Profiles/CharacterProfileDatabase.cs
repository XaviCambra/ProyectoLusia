// Runtime/Profiles/CharacterProfileDatabase.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        {
            DGLog.Err("FindById llamado con id vacío o null");
        }

        if (profiles == null || profiles.Count == 0)
        {
            DGLog.Err($"FindById('{profileId}') sin perfiles en la DB");
        }

        var pp = profiles.FirstOrDefault(x => x != null && x.ProfileId == profileId);
        DGLog.Info($"DB.FindById('{profileId}') → {(pp ? pp.name : "NULL")}");

        if (string.IsNullOrEmpty(profileId)) return null;
        if (_byId == null) BuildIndex();
        _byId.TryGetValue(profileId, out var p);
        return p;
    }
}

#if UNITY_EDITOR
public static class CharacterProfileDatabaseMenu
{
    [MenuItem("Dialogue/Profiles/Create or Update Database (scan project)")]
    public static void CreateOrUpdateDatabase()
    {
        // 1) Localiza o crea la DB en Resources/Dialogue/CharacterProfiles.asset
        const string folder = "Assets/Resources/Dialogue";
        const string assetPath = folder + "/CharacterProfiles.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "Dialogue");

        var db = AssetDatabase.LoadAssetAtPath<CharacterProfileDatabase>(assetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CharacterProfileDatabase>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        // 2) Encuentra todos los CharacterProfile del proyecto
        var guids = AssetDatabase.FindAssets("t:CharacterProfile");
        var all = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<CharacterProfile>(p))
            .Where(p => p != null)
            .ToList();

        // 3) Sustituye la lista y reconstruye índice
        db.profiles = all;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CharacterProfileDatabase] Escaneados {all.Count} perfiles y actualizada la base de datos en {assetPath}.");
    }
}
#endif

