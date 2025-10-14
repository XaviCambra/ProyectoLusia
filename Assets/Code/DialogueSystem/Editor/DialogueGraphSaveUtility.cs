// Editor/GraphView/DialogueGraphSaveUtility.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Guarda y carga el grafo desde/hacia el ScriptableObject (DialogueGraph).
/// </summary>
public static class DialogueGraphSaveUtility
{
    /// <summary>
    /// Serializa el contenido visual del GraphView hacia el asset ScriptableObject.
    /// </summary>
    public static void SaveGraph(DialogueGraphView view, DialogueGraph asset)
    {
        if (view == null || asset == null)
        {
            Debug.LogWarning("SaveGraph: view o asset nulos.");
            return;
        }

        var nodes = new List<DialogueNodeData>();
        var edges = new List<EdgeData>();

        // ✅ Nodos
        foreach (var node in view.nodes.ToList())
        {
            if (node is DialogueNodeView nodeView)
            {
                nodeView.Data.nodeRect = nodeView.GetPosition();
                nodes.Add(nodeView.Data);
            }
        }

        // ✅ Conexiones
        foreach (var e in view.edges.ToList())
        {
            if (e.output?.node is DialogueNodeView fromNode &&
                e.input?.node is DialogueNodeView toNode)
            {
                edges.Add(new EdgeData
                {
                    fromNodeGUID = fromNode.Data.GUID,
                    fromPortName = e.output.portName,
                    toNodeGUID = toNode.Data.GUID
                });
            }
        }

        asset.SetData(nodes, edges);
#if UNITY_EDITOR
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
#endif
        Debug.Log($"[DialogueGraphSaveUtility] Guardado: {nodes.Count} nodos, {edges.Count} conexiones.");
    }


    /// <summary>
    /// Carga desde el asset ScriptableObject y reconstruye el GraphView.
    /// </summary>
    public static void LoadGraph(DialogueGraphView view, DialogueGraph asset)
    {
        if (view == null || asset == null)
        {
            Debug.LogWarning("LoadGraph: view o asset nulos.");
            return;
        }

        // Limpia contenido actual (nodos + edges) de forma explícita
        var toRemove = new System.Collections.Generic.List<GraphElement>();
        toRemove.AddRange(view.edges.ToList());
        toRemove.AddRange(view.nodes.ToList());
        if (toRemove.Count > 0) view.DeleteElements(toRemove);
        view.ClearSelection();

        // Crea NodeViews, pero difiere SetPosition hasta después del layout
        var guidToView = new Dictionary<string, DialogueNodeView>();
        var pendingPositions = new List<(DialogueNodeView view, Rect rect)>();

        foreach (var n in asset.Nodes)
        {
            var nodeView = new DialogueNodeView(n);
            view.AddElement(nodeView);

            // Normaliza por si w/h vienen 0 desde un asset antiguo
            var r = n.nodeRect;
            if (r.width <= 1f || r.height <= 1f)
                r = new Rect(r.x, r.y, 320f, 180f);

            // NO setear aquí; se setea tras el layout
            pendingPositions.Add((nodeView, r));
            guidToView[n.GUID] = nodeView;
        }

        // Reconstruye edges
        foreach (var ed in asset.Edges)
        {
            if (!guidToView.TryGetValue(ed.fromNodeGUID, out var from)) continue;
            if (!guidToView.TryGetValue(ed.toNodeGUID, out var to)) continue;

            var outPort = FindOutputPort(from, ed.fromPortName);
            var inPort = to.inputContainer[0] as Port;
            if (outPort == null || inPort == null) continue;

            var edge = outPort.ConnectTo(inPort);
            view.AddElement(edge);
        }

        // Posiciona nodos tras el primer layout para que no se “reapilen”
        view.schedule.Execute(() =>
        {
            foreach (var (nv, rect) in pendingPositions)
                nv.SetPosition(rect);

            // opcional: encuadrar todo
            // view.FrameAll();
        }).ExecuteLater(0);

        Debug.Log($"[DialogueGraphSaveUtility] Cargado: {asset.Nodes.Count} nodos, {asset.Edges.Count} conexiones.");
    }

    /// <summary>
    /// Busca un puerto de salida por nombre en un DialogueNodeView.
    /// Soporta puertos directos ("Next") y puertos dentro de filas (nodos de elección).
    /// </summary>
    private static Port FindOutputPort(DialogueNodeView node, string portName)
    {
        foreach (var child in node.outputContainer.Children())
        {
            if (child is Port p && p.portName == portName)
                return p;

            // En nodos de elección, cada fila (row) contiene un TextField y un Port
            if (child is VisualElement row)
            {
                foreach (var sub in row.Children())
                {
                    if (sub is Port p2 && p2.portName == portName)
                        return p2;
                }
            }
        }
        return null;
    }
}
#endif
