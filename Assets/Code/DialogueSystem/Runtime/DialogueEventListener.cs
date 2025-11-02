using UnityEngine;

public class DialogueEventListener : MonoBehaviour
{
    [SerializeField] private string key = "OpenDoor";

    void OnEnable() => GlobalDialogueEvents.OnNodeEventPayload += OnEvt;
    void OnDisable() => GlobalDialogueEvents.OnNodeEventPayload -= OnEvt;

    private void OnEvt(DialogueEventPayload p)
    {
        // Filtra por clave
        if (p.key != key) return;

        Debug.Log($"Handling event for key: {key} (type={p.type})");

        // Lee el payload según el tipo definido en el nodo
        switch (p.type)
        {
            case EventPayloadType.Int:
                OpenDoor(Mathf.Max(0, p.intValue));
                break;

            case EventPayloadType.Float:
                OpenDoor(Mathf.Max(0, Mathf.RoundToInt(p.floatValue)));
                break;

            case EventPayloadType.String:
                if (int.TryParse(p.stringValue, out var speedFromString))
                    OpenDoor(Mathf.Max(0, speedFromString));
                else
                    OpenDoor(1); // fallback
                break;

            case EventPayloadType.Bool:
                OpenDoor(p.boolValue ? 1 : 0);
                break;

            case EventPayloadType.Char:
                // si es dígito '0'..'9', úsalo; si no, fallback
                int speedFromChar = char.IsDigit(p.charValue) ? (p.charValue - '0') : 1;
                OpenDoor(Mathf.Max(0, speedFromChar));
                break;

            default:
                OpenDoor(1);
                break;
        }
    }

    private void OpenDoor(int speed)
    {
        Debug.Log($"Door opened with speed: {speed}");
        // TODO: tu lógica real de apertura
    }
}
