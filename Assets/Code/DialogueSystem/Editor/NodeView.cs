using UnityEditor;
using UnityEngine;

namespace Kimera.NodeEditor
{
    internal class NodeView
    {
        private readonly NodeData _data;
        private readonly ResizeHandle _resizeHandle = new ResizeHandle();
        private bool _isDragging;
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartPos;

        // 👉 Evento para avisar que se solicita borrar este nodo
        public event System.Action<NodeView> OnRequestDelete;

        public NodeView(NodeData data)
        {
            _data = data;
            //if (_data.minWidthPerHeight > 0f)
            //    _sizeConstraint = new MinWidthOnlyConstraint(_data.minWidthPerHeight);
        }

        public int Id => _data.id;
        public Rect Rect => _data.rect;

        public Rect Draw()
        {
            _data.rect = GUI.Window(_data.id, _data.rect, DrawWindow, _data.title);
            return _data.rect;
        }

        private void DrawWindow(int windowId)
        {
            // --- Contenido ---
            EditorGUILayout.LabelField("Contenido del nodo", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(2);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("ID", _data.id);
            }

            EditorGUILayout.Space(6);

            // --- Botón de borrar ---
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("🗑 Borrar", GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Eliminar nodo",
                        $"¿Seguro que quieres eliminar '{_data.title}'?",
                        "Sí", "No"))
                    {
                        OnRequestDelete?.Invoke(this);
                    }
                }
            }

            // --- Redimensionar ---
            var localRect = new Rect(Vector2.zero, _data.rect.size);
            localRect = _resizeHandle.DoResize(localRect, _data.minSize, _data.maxSize);
            _data.rect.size = localRect.size;

            // --- Mover ---
            HandleDrag();
            GUI.DragWindow();
        }

        private void HandleDrag()
        {
            var e = Event.current;
            var windowLocal = new Rect(0, 0, _data.rect.width, _data.rect.height);

            if (e.type == EventType.MouseDown && e.button == 0 && windowLocal.Contains(e.mousePosition))
            {
                _isDragging = true;
                _dragStartMouse = GUIUtility.GUIToScreenPoint(e.mousePosition);
                _dragStartPos = _data.rect.position;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && _isDragging)
            {
                var mouseScreen = GUIUtility.GUIToScreenPoint(e.mousePosition);
                var delta = mouseScreen - _dragStartMouse;
                _data.rect.position = _dragStartPos + delta;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && _isDragging)
            {
                _isDragging = false;
                e.Use();
            }
        }
    }
}
