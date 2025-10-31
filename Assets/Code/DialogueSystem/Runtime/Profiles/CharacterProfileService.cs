// Runtime/Profiles/CharacterProfileService.cs
using System.Linq;
using UnityEngine;

public class CharacterProfileService : MonoBehaviour, ICharacterProfileService
{
    [Tooltip("Base de datos con todos los perfiles de personaje. Si no se asigna, se intentará resolver automáticamente.")]
    public CharacterProfileDatabase database;

    private static CharacterProfileDatabase _cached;

    private void Awake()
    {
        // 1) Si está asignada en el inspector, úsala
        if (database != null)
        {
            DGLog.Info($"ProfileService: usando DB asignada en Inspector: {database.name}", this);
            _cached = database;
            return;
        }

        // 2) Intentar cargar desde Resources (ruta recomendada)
        //    Crea un asset en: Assets/Resources/Dialogue/CharacterProfiles.asset
        database = Resources.Load<CharacterProfileDatabase>("Dialogue/CharacterProfiles");
        DGLog.Info($"ProfileService: Resources.Load → {(database ? database.name : "NULL")}", this);
        if (database != null) { _cached = database; return; }

#if UNITY_EDITOR
        // 3) En editor: intentar localizar cualquier asset en el proyecto
        //    (para evitar “me funciona en editor si me olvido del paso 2”)
        var guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterProfileDatabase");
        DGLog.Info($"ProfileService: Editor fallback. DB encontradas={guids.Length}");
        if (guids != null && guids.Length > 0)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            database = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterProfileDatabase>(path);
            DGLog.Info($"ProfileService: Editor cargó DB en {path} → {(database ? database.name : "NULL")}");
            if (database != null) { _cached = database; return; }
        }
#endif

        // 4) Si nada de lo anterior funcionó, deja _cached = null (fallará con error claro en GetById)
        DGLog.Err("ProfileService: no se pudo resolver ninguna DB (Inspector/Resources/Editor).");
        _cached = null;
    }

    public CharacterProfile GetById(string profileId)
    {
        DGLog.Info($"ProfileService.GetById('{profileId}')");
        if (_cached == null)
        {
            DGLog.Err("ProfileService no tiene DB (_cached null).");
            return null;
        }
        var result = _cached.FindById(profileId);
        if (result == null) DGLog.Err($"ProfileService.GetById: id='{profileId}' → NULL");
        return result;
    }
}
