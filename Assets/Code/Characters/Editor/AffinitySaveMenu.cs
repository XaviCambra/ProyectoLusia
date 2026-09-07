#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Atajo de Editor para borrar el guardado de afinidad de la escena abierta,
/// sin tener que ir a buscarlo a mano en AppData. Util al cambiar datos de
/// diseno (CharacterAffinityMap) durante pruebas: un guardado ya existente
/// pisa esos cambios hasta que se borra (es el comportamiento correcto de
/// un guardado real, pero estorba mientras se itera en el diseno).
/// </summary>
public static class AffinitySaveMenu
{
    [MenuItem("Tools/Afinidad/Borrar guardado de afinidad")]
    private static void DeleteSave()
    {
        var bootstrappers = Object.FindObjectsByType<AffinityServiceBootstrapper>(FindObjectsSortMode.None);
        if (bootstrappers.Length == 0)
        {
            Debug.LogWarning("[AffinitySaveMenu] No hay ningun AffinityServiceBootstrapper en la escena abierta.");
            return;
        }

        foreach (var bootstrapper in bootstrappers)
        {
            var path = Path.Combine(Application.persistentDataPath, bootstrapper.SaveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[AffinitySaveMenu] Borrado: {path}");
            }
            else
            {
                Debug.Log($"[AffinitySaveMenu] No existia (ya estaba limpio): {path}");
            }
        }
    }
}
#endif
