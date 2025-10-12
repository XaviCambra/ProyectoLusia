using UnityEditor;
using UnityEngine;

namespace Kimera.NodeEditor
{
    /// <summary>
    /// Comportamiento reusable de redimensionado con captura de ratón (hotControl).
    /// No sabe nada del modelo: solo opera sobre un Rect y límites.
    /// </summary>
    internal class ResizeHandle
    {
        private readonly int _controlId;
        private bool _isResizing;
        private Vector2 _startMouse;
        private Vector2 _startSize;
        private readonly float _gripSize;

        public ResizeHandle(float gripSize = 14f)
        {
            _controlId = GUIUtility.GetControlID("NodeResize".GetHashCode(), FocusType.Passive);
            _gripSize = gripSize;
        }

        /// <summary>
        /// Dibuja el grip y procesa eventos. Rect y límites están en coords locales de la ventana del nodo.
        /// </summary>
        public Rect DoResize(Rect localRect, Vector2 minSize, Vector2 maxSize)
        {
            var handleRect = new Rect(localRect.width - _gripSize, localRect.height - _gripSize, _gripSize, _gripSize);
            var e = Event.current;

            switch (e.GetTypeForControl(_controlId))
            {
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeUpLeft, _controlId);
                    DrawGrip(handleRect);
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && handleRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = _controlId;
                        _isResizing = true;
                        _startMouse = e.mousePosition;
                        _startSize = localRect.size;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == _controlId && _isResizing && e.button == 0)
                    {
                        Vector2 delta = e.mousePosition - _startMouse;
                        var newSize = _startSize + delta;

                        newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
                        newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);

                        localRect.size = newSize;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == _controlId && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        _isResizing = false;
                        e.Use();
                    }
                    break;

                case EventType.MouseLeaveWindow:
                    if (_isResizing) { GUIUtility.hotControl = 0; _isResizing = false; }
                    break;
            }
            return localRect;
        }

        private static void DrawGrip(Rect r)
        {
            var gripColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.35f) : new Color(0, 0, 0, 0.35f);
            Handles.BeginGUI();
            using (new Handles.DrawingScope(gripColor))
            {
                Vector3 p0 = new Vector3(r.xMin + 3, r.yMax - 3);
                Vector3 p1 = new Vector3(r.xMax - 3, r.yMin + 3);
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(new Vector3(p0.x + 4, p0.y), new Vector3(p1.x, p1.y + 4));
                Handles.DrawLine(new Vector3(p0.x, p0.y - 4), new Vector3(p1.x - 4, p1.y));
            }
            Handles.EndGUI();
        }
    }
}
