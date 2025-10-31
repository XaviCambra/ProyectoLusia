using System;
using UnityEngine;

[Serializable]
public struct DialogueEventPayload
{
    public string key;
    public EventPayloadType payloadType;

    public int intValue;
    public float floatValue;
    public string stringValue; // usado también como 'char' (string de 1)
    public bool boolValue;

    public override string ToString()
    {
        switch (payloadType)
        {
            case EventPayloadType.Int: return $"{key} => (int){intValue}";
            case EventPayloadType.Float: return $"{key} => (float){floatValue}";
            case EventPayloadType.String: return $"{key} => (string)\"{stringValue}\"";
            case EventPayloadType.Bool: return $"{key} => (bool){boolValue}";
            case EventPayloadType.Char: return $"{key} => (char)'{(string.IsNullOrEmpty(stringValue) ? '?' : stringValue[0])}'";
            default: return $"{key}";
        }
    }

    /// Helpers de lectura segura (opcionales)
    public bool TryGetInt(out int v) { v = intValue; return payloadType == EventPayloadType.Int; }
    public bool TryGetFloat(out float v) { v = floatValue; return payloadType == EventPayloadType.Float; }
    public bool TryGetString(out string v) { v = stringValue; return payloadType == EventPayloadType.String; }
    public bool TryGetBool(out bool v) { v = boolValue; return payloadType == EventPayloadType.Bool; }
    public bool TryGetChar(out char c)
    {
        c = default;
        if (payloadType != EventPayloadType.Char || string.IsNullOrEmpty(stringValue)) return false;
        c = stringValue[0];
        return true;
    }
}
