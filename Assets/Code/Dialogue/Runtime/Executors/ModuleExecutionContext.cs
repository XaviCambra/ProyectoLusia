using System.Threading;

/// <summary>
/// Contexto que el runner pasa a cada executor al ejecutar un módulo.
/// Contiene datos del nodo en curso que los executors pueden necesitar.
/// </summary>
public readonly struct ModuleExecutionContext
{
    /// <summary>GUID del nodo en ejecución.</summary>
    public readonly string NodeGuid;

    /// <summary>Token de cancelación. Se cancela cuando el runner navega a otro nodo.</summary>
    public readonly CancellationToken Token;

    public ModuleExecutionContext(string nodeGuid, CancellationToken token)
    {
        NodeGuid = nodeGuid;
        Token    = token;
    }
}
