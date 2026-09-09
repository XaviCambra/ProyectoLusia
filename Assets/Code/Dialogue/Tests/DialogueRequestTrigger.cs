using UnityEngine;

/// <summary>
/// Servicio/guia para pedir un dialogo desde cualquier script de gameplay (NPC, trigger,
/// boton de UI, Animation Event...) a traves de DialogueRequestSO. Se puede usar tal cual
/// (metodos publicos + tecla de atajo) o leer como ejemplo de como hacerlo desde cero.
/// </summary>
public class DialogueRequestTrigger : MonoBehaviour
{
    [SerializeField] private DialogueRequestSO request; // el canal compartido (DialogueRequestChannel.asset)
    [SerializeField] private DialogueGraph graph; // el dialogo que este trigger en concreto pide

    [Header("Atajo por teclado (opcional)")]
    [SerializeField] private bool useKeyTrigger = true;
    [SerializeField] private KeyCode key = KeyCode.G;

    /// <summary>True mientras el dialogo solicitado sigue en curso (aun no ha terminado).</summary>
    public bool IsDialogueActive => request != null && request.IsActive;

    private void OnEnable()
    {
        if (request != null) request.OnDialogueFinished += HandleDialogueFinished;
    }

    private void OnDisable()
    {
        if (request != null) request.OnDialogueFinished -= HandleDialogueFinished;
    }

    private void Update()
    {
        if (useKeyTrigger && Input.GetKeyDown(key))
            RequestDialogue();
    }

    /// <summary>Pide el DialogueGraph configurado en el Inspector.</summary>
    public bool RequestDialogue() => RequestDialogue(graph);

    /// <summary>Pide un DialogueGraph concreto, ignorando el configurado en el Inspector.</summary>
    public bool RequestDialogue(DialogueGraph graphToPlay)
    {
        // Comprobar que el campo 'request' esta asignado en el Inspector.
        if (request == null)
        {
            Debug.LogWarning($"[{name}] Falta 'request' en el Inspector.", this);
            return false;
        }

        // Comprobar que no hay ya otro dialogo en curso (solo se admite uno a la vez).
        if (request.IsActive)
        {
            Debug.Log($"[{name}] Ya hay un dialogo en curso, no se pide otro.", this);
            return false;
        }

        // Pedir el dialogo. Devuelve true si se acepto y empezo a cargar la escena de
        // Dialogo, false si se rechazo (por ejemplo 'graphToPlay' nulo).
        bool started = request.RequestDialogue(graphToPlay);
        Debug.Log(started
            ? $"[{name}] Dialogo '{graphToPlay?.name}' solicitado."
            : $"[{name}] Solicitud rechazada.", this);

        return started;
    }

    /// <summary>Se llama sola cuando el dialogo pedido termina y su escena ya se descargo.</summary>
    private void HandleDialogueFinished()
    {
        Debug.Log($"[{name}] Dialogo terminado.", this);
    }
}
