using UnityEngine;

/// <summary>
/// Modulo complementario de Interactable: al accionar, pide reproducir un DialogueGraph en la
/// escena de Dialogo compartida, a traves del mismo canal (DialogueRequestSO) que ya usa
/// DialogueRequestTrigger. Pensado para NPCs u objetos del mundo que abren un dialogo al
/// interactuar, reusando la deteccion/tecla ya comunes del sistema Interactable en vez de
/// tener su propio KeyCode.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class InteractableDialogueModule : MonoBehaviour
{
    [Header("Dialogo")]
    [Tooltip("Canal de solicitud compartido (el mismo asset DialogueRequestChannel que usa DialogueRequestTrigger, no crees uno nuevo).")]
    public DialogueRequestSO request;
    [Tooltip("Dialogo que se pide al interactuar.")]
    public DialogueGraph graph;

    /// <summary>True mientras el dialogo pedido por este modulo sigue en curso.</summary>
    public bool IsDialogueActive => request != null && request.IsActive;

    Interactable _interactable;
    InteractableKeyHintModule _keyHint;

    void Awake()
    {
        _interactable = GetComponent<Interactable>();
        _keyHint = GetComponent<InteractableKeyHintModule>();
    }

    void OnEnable()
    {
        if (!_interactable) _interactable = GetComponent<Interactable>();
        _interactable.OnInteract += HandleInteract;

        if (request != null) request.OnDialogueFinished += HandleDialogueFinished;
    }

    void OnDisable()
    {
        if (_interactable) _interactable.OnInteract -= HandleInteract;
        if (request != null) request.OnDialogueFinished -= HandleDialogueFinished;
    }

    void HandleInteract(GameObject character)
    {
        if (request == null)
        {
            Debug.LogWarning($"[{name}] Falta 'request' en el Inspector.", this);
            return;
        }

        if (request.IsActive)
        {
            Debug.Log($"[{name}] Ya hay un dialogo en curso, no se pide otro.", this);
            return;
        }

        bool started = request.RequestDialogue(graph);
        Debug.Log(started
            ? $"[{name}] Dialogo '{graph?.name}' solicitado."
            : $"[{name}] Solicitud rechazada.", this);

        if (started) _keyHint?.SetSuppressed(true);
    }

    void HandleDialogueFinished()
    {
        Debug.Log($"[{name}] Dialogo terminado.", this);
        _keyHint?.SetSuppressed(false);
    }
}
