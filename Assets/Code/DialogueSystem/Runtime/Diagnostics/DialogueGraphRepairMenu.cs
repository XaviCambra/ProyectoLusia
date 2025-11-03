#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DialogueGraphRepairMenu
{
    [MenuItem("Dialogue/Repair/Sync Node profileId from profileRef (selected graphs)")]
    public static void SyncProfileIdsOnSelectedGraphs()
    {
        var objs = Selection.objects;
        int graphs = 0, nodes = 0, changed = 0;

        foreach (var o in objs)
        {
            if (o is DialogueGraph g && g.Nodes != null)
            {
                graphs++;
                foreach (var n in g.Nodes)
                {
                    nodes++;
                    if (n.profileRef != null && n.profileId != n.profileRef.ProfileId)
                    {
                        n.profileId = n.profileRef.ProfileId;
                        changed++;
                    }
                }
                EditorUtility.SetDirty(g);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Repair] Graphs={graphs}, Nodes={nodes}, Fixed IDs={changed}");
    }
}
#endif
