using System.Threading;

/// <summary>
/// Contexto que el runner pasa a cada executor al ejecutar un módulo.
/// Contiene datos del nodo en curso que los executors pueden necesitar.
/// Es una clase (no struct): su estado se comparte y puede mutar entre
/// módulos del mismo nodo (ej. <see cref="CurrentProfile"/>).
/// </summary>
public sealed class ModuleExecutionContext
{
    /// <summary>GUID del nodo en ejecución.</summary>
    public readonly string NodeGuid;

    /// <summary>Token de cancelación. Se cancela cuando el runner navega a otro nodo.</summary>
    public readonly CancellationToken Token;

    /// <summary>
    /// Personaje "actual" para los módulos siguientes dentro del mismo nodo
    /// (ej. lo fija un módulo de perfil para que un módulo de texto posterior
    /// lo use como hablante/avatar). Los executors que no lo necesiten lo ignoran.
    /// </summary>
    public CharacterDefinition CurrentProfile { get; set; }

    public ModuleExecutionContext(string nodeGuid, CancellationToken token)
    {
        NodeGuid = nodeGuid;
        Token    = token;
    }
}
