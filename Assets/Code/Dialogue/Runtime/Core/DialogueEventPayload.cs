// ========================= Payload de eventos =========================
using System;

[Serializable]
public struct DialogueEventPayload
{
    public string key;
    public EventPayloadType type;

    // Valores posibles (se usa el que corresponda con 'type')
    public int intValue;
    public float floatValue;
    public string stringValue;
    public bool boolValue;
    public char charValue;

    public override string ToString()
    {
        return type switch
        {
            EventPayloadType.Int => $"[{key}] Int={intValue}",
            EventPayloadType.Float => $"[{key}] Float={floatValue}",
            EventPayloadType.String => $"[{key}] String=\"{stringValue}\"",
            EventPayloadType.Bool => $"[{key}] Bool={boolValue}",
            EventPayloadType.Char => $"[{key}] Char='{charValue}'",
            _ => $"[{key}] (none)"
        };
    }
}