using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kimera.NodeEditor
{
    public class SimpleNodeEditorWindow : EditorWindow
    {
        private readonly List<NodeData> _nodes = new();
        private readonly Dictionary<int, NodeView> _views = new();
        private int _nextId = 1;

        private Vector2 _canvasScroll;
        private Vector2 _lastMouseCanvas;

        [MenuItem("Tools/Kimera/Simple Node Editor")]
        public static void Open()
        {
            var wnd = GetWindow<SimpleNodeEditorWindow>("Simple Node Editor");
            wnd.minSize = new Vector2(600, 380);
        }

        private void OnGUI()
        {
            DrawToolbar();

            var viewRect = new Rect(0, 18, position.width, position.height - 18);
            var canvasRect = new Rect(0, 0, Mathf.Max(position.width, 4000), Mathf.Max(position.height, 4000));

            _canvasScroll = GUI.BeginScrollView(viewRect, _canvasScroll, canvasRect, true, true);
            _lastMouseCanvas = Event.current.mousePosition + _canvasScroll;

            DrawGrid(canvasRect, 20f, 0.15f);
            DrawGrid(canvasRect, 100f, 0.08f);

            BeginWindows();
            bool anyResize = false;

            // 🔧 Dibujo y gestión de eventos
            foreach (var n in _nodes.ToArray())
            {
                if (!_views.TryGetValue(n.id, out var view))
                {
                    view = new NodeView(n);
                    view.OnRequestDelete += OnDeleteNodeRequest; // 👈 escuchar evento
                    _views[n.id] = view;
                }

                var before = n.rect.size;
                view.Draw();
                if (n.rect.size != before) anyResize = true;
            }

            EndWindows();
            GUI.EndScrollView();

            if (anyResize) Repaint();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("➕ Crear nodo", EditorStyles.toolbarButton))
                    CreateNodeAtMouse();

                if (GUILayout.Button("🗑 Borrar todos", EditorStyles.toolbarButton))
                {
                    if (EditorUtility.DisplayDialog("Borrar todos",
                        "¿Seguro que quieres borrar todos los nodos?", "Sí", "No"))
                    {
                        _nodes.Clear();
                        _views.Clear();
                        _nextId = 1;
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"Nodos: {_nodes.Count}", EditorStyles.miniLabel);
            }
        }

        private void CreateNodeAtMouse()
        {
            var rect = new Rect(_lastMouseCanvas.x - 110, _lastMouseCanvas.y - 60, 220, 120);
            var data = new NodeData
            {
                id = _nextId++,
                title = $"Nodo {_nextId - 1}",
                rect = rect
            };
            _nodes.Add(data);
        }

        // 🔥 Elimina un nodo (llamado desde NodeView)
        private void OnDeleteNodeRequest(NodeView view)
        {
            if (_views.ContainsKey(view.Id))
                _views.Remove(view.Id);

            var nodeData = _nodes.Find(n => n.id == view.Id);
            if (nodeData != null)
                _nodes.Remove(nodeData);

            Repaint();
        }

        private static void DrawGrid(Rect rect, float spacing, float opacity)
        {
            int w = Mathf.CeilToInt(rect.width / spacing);
            int h = Mathf.CeilToInt(rect.height / spacing);

            Handles.BeginGUI();
            var color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, opacity) : new Color(0, 0, 0, opacity);
            using (new Handles.DrawingScope(color))
            {
                for (int i = 0; i < w; i++)
                    Handles.DrawLine(new Vector3(i * spacing, 0), new Vector3(i * spacing, rect.height));
                for (int j = 0; j < h; j++)
                    Handles.DrawLine(new Vector3(0, j * spacing), new Vector3(rect.width, j * spacing));
            }
            Handles.EndGUI();
        }
    }
}
