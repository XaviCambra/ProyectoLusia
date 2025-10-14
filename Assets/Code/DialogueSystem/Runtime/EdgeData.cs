// Runtime/LegacySoModel/EdgeData.cs
using System;

/// <summary>
/// Representa una conexión entre dos nodos dentro del asset DialogueGraph.
/// </summary>
[Serializable]
public class EdgeData
{
    /// <summary>GUID del nodo origen.</summary>
    public string fromNodeGUID;

    /// <summary>Nombre del puerto de salida del nodo origen (por ejemplo, "Next" o el ID de una opción).</summary>
    public string fromPortName;

    /// <summary>GUID del nodo destino.</summary>
    public string toNodeGUID;
}
