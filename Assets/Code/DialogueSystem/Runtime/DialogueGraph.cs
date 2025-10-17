// Runtime/LegacySoModel/DialogueGraph.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Asset principal del grafo de diálogos usado por el editor (GraphView).
/// Almacena nodos y conexiones, y expone SetData para que el editor guarde.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Dialogue Graph", fileName = "NewDialogueGraph")]
public class DialogueGraph : ScriptableObject
{
    [SerializeField] private List<DialogueNodeData> nodes = new();
    [SerializeField] private List<EdgeData> edges = new();

    public IReadOnlyList<DialogueNodeData> Nodes => nodes;
    public IReadOnlyList<EdgeData> Edges => edges;

    /// <summary>
    /// Reemplaza el contenido del grafo (usado por el editor al guardar).
    /// </summary>
    public void SetData(List<DialogueNodeData> nodeList, List<EdgeData> edgeList)
    {
        nodes = nodeList ?? new();
        edges = edgeList ?? new();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Devuelve el nodo por GUID o null si no existe.
    /// </summary>
    public DialogueNodeData FindNode(string guid)
    {
        return nodes.Find(n => n.GUID == guid);
    }
}
