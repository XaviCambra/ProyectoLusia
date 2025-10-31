#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Dialogue_MigrateProfileIds
{
    [MenuItem("Dialogue/Diagnostics/Repair Node ProfileIds (from profileRef)")]
    public static void RepairAllGraphs()
    {
        var graphGuids = AssetDatabase.FindAssets("t:DialogueGraph");
        int graphs = 0, nodes = 0, fixedIds = 0, cleared = 0;

        foreach (var g in graphGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
            if (graph == null) continue;
            graphs++;

            // Asegúrate de poder acceder a los nodos (ajusta si tu graph los guarda en otra propiedad)
            var allNodes = graph.Nodes?.ToList();
            if (allNodes == null) continue;

            foreach (var n in allNodes)
            {
                var data = n as DialogueNodeData;
                if (data == null) continue;
                nodes++;

                // Caso 1: profileRef existe → impone el profileId correcto
                if (data.profileRef != null)
                {
                    var newId = data.profileRef.ProfileId;
                    if (string.IsNullOrEmpty(data.profileId) || data.profileId != newId)
                    {
                        DGLog.Info($"[Repair] {graph.name}:{data.GUID} → set profileId '{newId}' from ref '{data.profileRef.name}'");
                        data.profileId = newId;
                        EditorUtility.SetDirty(graph);
                        fixedIds++;
                    }
                }
                // Caso 2: no hay ref y el id apunta a nada → limpia para que se vea el problema claramente
                else if (!string.IsNullOrEmpty(data.profileId))
                {
                    // si ese id ya no existe en la DB actual, mejor limpiar
                    var db = Resources.Load<CharacterProfileDatabase>("Dialogue/CharacterProfiles");
                    var exists = db != null && db.FindById(data.profileId) != null;
                    if (!exists)
                    {
                        DGLog.Warn($"[Repair] {graph.name}:{data.GUID} id '{data.profileId}' no existe en DB → clear");
                        data.profileId = null;
                        EditorUtility.SetDirty(graph);
                        cleared++;
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[DG][Repair Node ProfileIds] graphs={graphs} nodes={nodes} fixedIds={fixedIds} cleared={cleared}");
    }
}
#endif
