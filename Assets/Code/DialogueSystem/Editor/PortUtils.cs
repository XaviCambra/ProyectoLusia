// Editor/GraphView/PortUtils.cs
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

/// <summary>
/// Utilidad para crear puertos (input/output) de forma sencilla dentro del GraphView.
/// </summary>
public static class PortUtils
{
    /// <summary>
    /// Crea un puerto genérico para un Node.
    /// </summary>
    /// <param name="node">Node propietario del puerto.</param>
    /// <param name="direction">Input u Output.</param>
    /// <param name="capacity">Single o Multi.</param>
    /// <param name="portName">Nombre visible del puerto.</param>
    public static Port CreatePort(Node node, Direction direction, Port.Capacity capacity, string portName)
    {
        // El tipo (typeof(float)) es un placeholder requerido por la API de GraphView.
        var port = node.InstantiatePort(
            Orientation.Horizontal,
            direction,
            capacity,
            typeof(float)
        );

        port.portName = portName;
        return port;
    }
}
#endif
