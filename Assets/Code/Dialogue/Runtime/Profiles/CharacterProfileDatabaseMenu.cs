#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterProfileDatabaseMenu
{
    [MenuItem("Dialogue/Profiles/Create or Update Database (scan project)")]
    public static void CreateOrUpdateDatabase()
    {
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

        var guids = AssetDatabase.FindAssets("t:CharacterProfile");
        var all = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<CharacterProfile>(p))
            .Where(p => p != null)
            .ToList();

        db.profiles = all;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CharacterProfileDatabase] Escaneados {all.Count} perfiles y actualizada la base de datos en {assetPath}.");
    }
}
#endif
