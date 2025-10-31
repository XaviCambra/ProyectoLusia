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

    // Nuevo: clave + payload
    public static event Action<DialogueEventPayload> OnNodeEventPayload;

    /// <summary>
    /// Dispara un evento global para la clave especificada.
    /// </summary>
    public static void Fire(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
            return;

        OnNodeEvent?.Invoke(eventKey);
    }

    public static void Fire(DialogueEventPayload payload)
    {
        OnNodeEventPayload?.Invoke(payload);
        // Compatibilidad: sigue notificando por clave para oyentes antiguos
        if (!string.IsNullOrEmpty(payload.key))
            OnNodeEvent?.Invoke(payload.key);
    }
}
