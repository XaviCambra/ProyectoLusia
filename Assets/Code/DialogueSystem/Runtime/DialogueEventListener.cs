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

        switch (p.type)
        {
            case EventPayloadType.Int:
                OpenDoorInt(p.intValue);
                break;

            case EventPayloadType.Float:
                OpenDoorFloat(p.floatValue);
                break;

            case EventPayloadType.String:
                OpenDoorString(p.stringValue);
                break;

            case EventPayloadType.Bool:
                OpenDoorBool(p.boolValue);
                break;

            case EventPayloadType.Char:
                OpenDoorChar(p.charValue);
                break;

            default:
                OpenDoor();
                break;
        }
    }

    private void OpenDoor() => Debug.Log($"ANY TYPE value");
    private void OpenDoorInt(int v) => Debug.Log($"INT TYPE value: {v}");
    private void OpenDoorFloat(float v) => Debug.Log($"FLOAT TYPE value: {v}");
    private void OpenDoorString(string v) => Debug.Log($"STRING TYPE value: {v}");
    private void OpenDoorBool(bool v) => Debug.Log($"BOOL TYPE value: {v}");
    private void OpenDoorChar(char v) => Debug.Log($"CHAR TYPE value: {v}");
}
