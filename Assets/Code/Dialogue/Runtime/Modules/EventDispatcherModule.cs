using System;
using UnityEngine;

// USO: Solo sistema de Retratos (DialogueRunner). Ignorado por Chat Bubble.
/// <summary>
/// Módulo de evento: dispara un evento global con payload tipado
/// cuando el nodo llega a este módulo en su ejecución.
/// Por defecto no es bloqueante (fire and forget).
/// </summary>
[Serializable]
public class EventDispatcherModule : DialogueModuleBase
{
    public override string DisplayName => "Event";

    public EventDispatcherModule()
    {
        runMode = ModuleRunMode.FireAndForget; // Los eventos no bloquean por defecto
    }

    [Tooltip("Clave del evento. Los sistemas de juego escuchan esta clave.")]
    public string eventKey = "";

    [Tooltip("Tipo del payload que acompaña al evento.")]
    public EventPayloadType payloadType = EventPayloadType.None;

    [Tooltip("Valor entero del payload (si payloadType = Int).")]
    public int intValue = 0;

    [Tooltip("Valor float del payload (si payloadType = Float).")]
    public float floatValue = 0f;

    [Tooltip("Valor string del payload (si payloadType = String o Char).")]
    public string stringValue = "";

    [Tooltip("Valor booleano del payload (si payloadType = Bool).")]
    public bool boolValue = false;

    /// <summary>Construye el payload tipado listo para disparar.</summary>
    public DialogueEventPayload BuildPayload()
    {
        var payload = new DialogueEventPayload
        {
            key  = eventKey,
            type = payloadType
        };

        switch (payloadType)
        {
            case EventPayloadType.Int:    payload.intValue    = intValue;   break;
            case EventPayloadType.Float:  payload.floatValue  = floatValue; break;
            case EventPayloadType.String: payload.stringValue = stringValue ?? string.Empty; break;
            case EventPayloadType.Bool:   payload.boolValue   = boolValue;  break;
            case EventPayloadType.Char:
                payload.charValue = !string.IsNullOrEmpty(stringValue) ? stringValue[0] : '\0';
                break;
        }
        return payload;
    }
}
