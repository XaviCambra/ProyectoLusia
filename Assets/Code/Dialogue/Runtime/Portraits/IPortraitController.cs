using System.Threading.Tasks;

/// <summary>
/// Controla el pool de retratos, su colocación en escena y las animaciones asociadas.
/// Implementación de referencia: <see cref="PortraitController"/>.
/// </summary>
public interface IPortraitController
{
    /// <summary>
    /// Inicializa el pool escaneando los <see cref="PortraitModule"/> del grafo.
    /// Debe poder llamarse múltiples veces (reconstruye el pool).
    /// </summary>
    void Init(DialogueGraph graph);

    /// <summary>
    /// Aplica el estado visual de un módulo de retrato: sprite, orden visual, tinte/escala
    /// y animación de colocación/entrada. Devuelve cuando la colocación principal ha finalizado.
    /// </summary>
    Task ApplyAsync(PortraitModule module);

    /// <summary>
    /// Detiene y limpia toda animación activa, oculta retratos y restablece el estado inicial.
    /// </summary>
    void ResetAll();

    /// <summary>
    /// Inicia la animación especial (Playable) en el portrait del perfil indicado.
    /// </summary>
    void PlaySpecialAnimation(CharacterDefinition profile, UnityEngine.AnimationClip clip, float speed, bool loop, bool persistent = false);

    /// <summary>
    /// Detiene la animación especial del portrait del perfil indicado.
    /// </summary>
    void StopSpecialAnimation(CharacterDefinition profile);

    /// <summary>
    /// Teleporta el portrait al estado final definido por el módulo, deteniendo
    /// cualquier animación de colocación activa. Funciona independientemente del caché interno.
    /// </summary>
    void SnapPlacement(PortraitModule module);
}
