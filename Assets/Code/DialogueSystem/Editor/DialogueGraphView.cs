// Editor/GraphView/DialogueGraphView.cs
#if UNITY_EDITOR
using System.Collections.Generic;
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

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatible = new List<Port>();
        foreach (var port in ports)
        {
            if (port == startPort) continue;
            if (port.node == startPort.node) continue;
            if (port.direction == startPort.direction) continue;
            compatible.Add(port);
        }
        return compatible;
    }

    public DialogueNodeView CreateNodeAtCenter(DialogueNodeData data = null)
    {
        if (data == null) data = new DialogueNodeData();
        var nodeView = new DialogueNodeView(data);
        AddElement(nodeView);

        var worldCenter = new Vector2(
            layout.width > 0 ? layout.width * 0.5f : 400f,
            layout.height > 0 ? layout.height * 0.5f : 250f
        );
        var localCenter = contentViewContainer.WorldToLocal(worldCenter);
        nodeView.SetPosition(new Rect(localCenter, nodeView.DefaultSize));
        return nodeView;
    }
}
#endif
