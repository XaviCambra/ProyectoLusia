// Editor/GraphView/DialogueGraphView.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    public DialogueGraphEditorWindow EditorWindow { get; }

    public DialogueGraphView(DialogueGraphEditorWindow editorWindow)
    {
        EditorWindow = editorWindow;
        style.flexGrow = 1;

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        graphViewChanged = (change) =>
        {
            if (change.movedElements != null)
            {
                foreach (var el in change.movedElements)
                    if (el is DialogueNodeView nv)
                        nv.Data.nodeRect = nv.GetPosition();
            }
            return change;
        };
    }

    //public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    //{
    //    var compatible = new List<Port>();
    //    foreach (var port in ports)
    //    {
    //        if (port == startPort) continue;
    //        if (port.node == startPort.node) continue;
    //        if (port.direction == startPort.direction) continue;
    //        compatible.Add(port);
    //    }
    //    return compatible;
    //}

    public DialogueNodeView CreateNodeAtCenter(DialogueNodeData data = null)
    {
        if (data == null) data = new DialogueNodeData();
        var nodeView = new DialogueNodeView(data);
        AddElement(nodeView);

        nodeView.OnDataChanged = () => EditorWindow.MarkAssetDirtyAndSave();

        var worldCenter = new Vector2(
            layout.width > 0 ? layout.width * 0.5f : 400f,
            layout.height > 0 ? layout.height * 0.5f : 250f
        );
        var localCenter = contentViewContainer.WorldToLocal(worldCenter);
        nodeView.SetPosition(new Rect(localCenter, nodeView.DefaultSize));
        return nodeView;
    }

    // --- BACKDROP FRAMES ---

    // Crea un marco centrado en la vista actual
    public BackdropFrameView CreateBackdropFrameCentered(string title = "Frame")
    {
        var r = contentViewContainer.WorldToLocal(new Vector2(layout.width, layout.height) * 0.5f);
        var data = new BackdropFrameData(
            title,
            new Color(0.2f, 0.6f, 1f, 0.15f),
            new Rect(r.x - 200, r.y - 120, 400, 240)
        );
        var view = new BackdropFrameView(data);
        AddElement(view);
        view.SendToBack(); // detrás de nodos
        return view;
    }

    // Cargar desde modelo (tras nodos/edges)
    public void LoadBackdropFrames(IReadOnlyList<BackdropFrameData> frames)
    {
        if (frames == null) return;
        foreach (var f in frames)
        {
            var v = new BackdropFrameView(f);
            AddElement(v);
            v.SendToBack();
        }
        EnsureFramesBehindNodes();
    }

    // Para guardar
    public List<BackdropFrameData> CollectBackdropFrames()
    {
        return graphElements
            .OfType<BackdropFrameView>()
            .Select(f => f.Data)
            .ToList();
    }

    // Asegurar que quedan al fondo
    public void EnsureFramesBehindNodes()
    {
        foreach (var f in graphElements.OfType<BackdropFrameView>())
        {
            f.layer = -1;
            f.SendToBack();
        }
    }
}
#endif
