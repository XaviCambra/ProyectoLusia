using UnityEngine;

/// <summary>
/// Bloquea el movimiento del jugador mientras hay un dialogo en curso y lo desbloquea cuando
/// termina, escuchando el mismo canal (DialogueRequestSO) que ya usa InteractableDialogueModule
/// para pedir la escena de Dialogo compartida. Vive en el mismo GameObject que el controlador
/// de movimiento (cualquier componente que implemente IMovementLockable) -- no conoce
/// SimpleWalker2D en concreto, asi que cambiar de controlador el dia de manana no le afecta.
/// </summary>
public sealed class PlayerDialogueMovementLock : MonoBehaviour
{
    [Tooltip("Canal de solicitud compartido (el mismo asset DialogueRequestChannel que usa el resto del proyecto).")]
    [SerializeField] private DialogueRequestSO request;

    private IMovementLockable _movement;

    private void Awake()
    {
        _movement = GetComponent<IMovementLockable>();
        if (_movement == null)
            Debug.LogWarning($"[{name}] Ningun componente de este GameObject implementa IMovementLockable.", this);
    }

    private void OnEnable()
    {
        if (request == null)
        {
            Debug.LogWarning($"[{name}] Falta 'request' en el Inspector.", this);
            return;
        }

        request.OnDialogueStarted  += HandleDialogueStarted;
        request.OnDialogueFinished += HandleDialogueFinished;
    }

    private void OnDisable()
    {
        if (request == null) return;

        request.OnDialogueStarted  -= HandleDialogueStarted;
        request.OnDialogueFinished -= HandleDialogueFinished;
    }

    private void HandleDialogueStarted()  => _movement?.SetMovementLocked(true);
    private void HandleDialogueFinished() => _movement?.SetMovementLocked(false);
}
