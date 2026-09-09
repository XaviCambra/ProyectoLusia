#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea el asset de DialogueRequestSO si todavia no existe. Escribirlo a mano por YAML es
/// fragil (su .meta con GUID solo existe tras el primer import de Unity), asi que se crea
/// aqui con AssetDatabase, mismo enfoque que AffinitySaveMenu.
/// </summary>
public static class DialogueRequestChannelMenu
{
    private const string TargetFolder = "Assets/Resource/GameData/Dialogs";
    private const string AssetPath    = TargetFolder + "/DialogueRequestChannel.asset";

    [MenuItem("Tools/Dialogo/Crear canal de solicitud de dialogo")]
    private static void CreateChannel()
    {
        if (AssetDatabase.LoadAssetAtPath<DialogueRequestSO>(AssetPath) != null)
        {
            Debug.Log($"[DialogueRequestChannelMenu] Ya existe: {AssetPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DialogueRequestSO>(AssetPath);
            return;
        }

        if (!Directory.Exists(TargetFolder))
            Directory.CreateDirectory(TargetFolder);

        var channel = ScriptableObject.CreateInstance<DialogueRequestSO>();
        AssetDatabase.CreateAsset(channel, AssetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DialogueRequestChannelMenu] Creado: {AssetPath}");
        Selection.activeObject = channel;
    }
}
#endif
