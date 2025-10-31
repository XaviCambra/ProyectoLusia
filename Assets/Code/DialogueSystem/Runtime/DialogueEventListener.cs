using UnityEngine;

public class DialogueEventListener : MonoBehaviour
{
    [SerializeField] private string key = "OpenDoor";

    void OnEnable() => GlobalDialogueEvents.OnNodeEventPayload += OnEvt;
    void OnDisable() => GlobalDialogueEvents.OnNodeEventPayload -= OnEvt;

    private void OnEvt(DialogueEventPayload p)
    {
        Debug.Log($"DialogueEventListener received event: {p}");

        if (p.key != key) return;

        Debug.Log($"Handling event for key: {key}");

        // Soporta distintos tipos según el diseño del nodo:
        if (p.payloadType == EventPayloadType.Int && p.TryGetInt(out var speed))
        {
            OpenDoor(speed); // usa el float
        }
        else
        {
            OpenDoor(1); // valor por defecto
        }
    }

    void OpenDoor(int speed)
    {
        Debug.Log($"Door opened with speed: {speed}");
    }
}
