using UnityEngine;

public class DialogueEventListener : MonoBehaviour
{
    [SerializeField] private string listenKey = "OpenDoor"; // la clave a escuchar

    private void OnEnable()
    {
        GlobalDialogueEvents.OnNodeEvent += HandleNodeEvent;
    }

    private void OnDisable()
    {
        GlobalDialogueEvents.OnNodeEvent -= HandleNodeEvent;
    }

    private void HandleNodeEvent(string key)
    {
        if (key == listenKey)
        {
            // Aquí ejecutas lo que toque (abrir puerta, activar cutscene, etc.)
            Debug.Log($"[DialogueEventListener] Recibido evento: {key}");
        }
    }
}
