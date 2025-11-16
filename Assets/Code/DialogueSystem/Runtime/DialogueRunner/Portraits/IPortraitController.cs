using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla el pool de retratos, su colocación en escena y las animaciones asociadas.
/// Implementación de referencia: <see cref="PortraitController"/>.
/// </summary>
public interface IPortraitController
{
    /// <summary>
    /// Inicializa el pool y lookups necesarios en base al <see cref="DialogueGraph"/> activo.
    /// Debe poder llamarse múltiples veces (reconstruye el pool).
    /// </summary>
    void Init(DialogueGraph graph);

    /// <summary>
    /// Aplica el estado visual de un nodo: sprite, orden visual, tinte/escala, 
    /// y animación de colocación/entrada-salida. Devuelve cuando la colocación principal ha finalizado.
    /// </summary>
    Task ApplyAsync(DialogueNodeData node);

    /// <summary>
    /// Aviso de que el texto del nodo va a empezar a mostrarse. Útil si
    /// la animación especial está configurada para iniciarse en este momento.
    /// </summary>
    void OnTextStart(DialogueNodeData node);

    /// <summary>
    /// Detiene y limpia toda animación activa, oculta retratos y restablece el estado inicial.
    /// </summary>
    void ResetAll();
}
