using System;
using UnityEngine;

namespace Kimera.NodeEditor
{
    [Serializable]
    public class NodeData
    {
        public int id;
        public string title = "Nodo";
        public Rect rect = new Rect(100, 100, 220, 120);

        // Límites de tamano (logica de negocio, no de GUI)
        public Vector2 minSize = new Vector2(220, 100);
        public Vector2 maxSize = new Vector2(400, 800);
    }
}
