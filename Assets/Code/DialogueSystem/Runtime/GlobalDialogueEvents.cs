// Runtime/LegacySoModel/GlobalDialogueEvents.cs
using System;

/// <summary>
/// Sistema global de eventos de diálogo.
/// Permite que cualquier parte del juego escuche o dispare eventos
/// definidos en los nodos mediante su clave (eventKey).
/// </summary>
public static class GlobalDialogueEvents
{
    /// <summary>
    /// Se lanza cuando un nodo dispara su evento (por su eventKey).
    /// </summary>
    public static event Action<string> OnNodeEvent;

    /// <summary>
    /// Dispara un evento global para la clave especificada.
    /// </summary>
    public static void Fire(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
            return;

        OnNodeEvent?.Invoke(eventKey);
    }
}
