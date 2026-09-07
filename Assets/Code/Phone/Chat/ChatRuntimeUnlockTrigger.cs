using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desbloquea (ChatConversation.Unlock) una o varias conversaciones de chat ya
/// presentes en el registry — pre-autoradas en el Inspector, ocultas hasta ahora
/// porque su condicion inicial es false. Nunca crea ni modifica ChatContact ni
/// ChatConversation como assets: solo cambia su estado de sesion no serializado,
/// para no dejar el registry "sucio" con progreso de una partida concreta.
///
/// Se puede disparar de varias formas a la vez, todas opcionales:
///  - Pulsando una tecla (key).
///  - Cuando se lanza un SignalSO (signal), igual que WaitForSignalModule.
///  - Cuando se dispara un evento global de dialogo con esa eventKey (el mismo
///    canal que usa EventDispatcherModule desde un nodo de un grafo).
///  - Llamando a Trigger() directamente desde otro script, un UnityEvent de
///    boton, un Animation Event, etc.
/// </summary>
public class ChatRuntimeUnlockTrigger : MonoBehaviour
{
    [Header("Chat a modificar")]
    [SerializeField] private ChatPhoneApp chatApp;

    [Tooltip("Conversaciones ya pre-autoradas en algun ChatContact del registry que se desbloquean al dispararse.")]
    [SerializeField] private List<ChatConversation> conversationsToUnlock = new();

    [Header("Disparadores (todos opcionales, se pueden combinar)")]
    [SerializeField] private KeyCode key = KeyCode.None;
    [SerializeField] private SignalSO signal;
    [Tooltip("Se dispara al recibir este eventKey por GlobalDialogueEvents (por ejemplo desde un EventDispatcherModule).")]
    [SerializeField] private string eventKey;
    [Tooltip("Si esta marcado, solo se ejecuta la primera vez que se cumple cualquiera de los disparadores.")]
    [SerializeField] private bool triggerOnce = true;

    private bool _fired;

    private void OnEnable()
    {
        if (signal != null)
            signal.OnRaised += Trigger;

        if (!string.IsNullOrEmpty(eventKey))
            GlobalDialogueEvents.OnNodeEvent += OnGlobalEvent;
    }

    private void OnDisable()
    {
        if (signal != null)
            signal.OnRaised -= Trigger;

        if (!string.IsNullOrEmpty(eventKey))
            GlobalDialogueEvents.OnNodeEvent -= OnGlobalEvent;
    }

    private void Update()
    {
        if (key != KeyCode.None && Input.GetKeyDown(key))
            Trigger();
    }

    private void OnGlobalEvent(string firedKey)
    {
        if (firedKey == eventKey)
            Trigger();
    }

    /// <summary>Ejecuta el desbloqueo configurado. Publico para poder llamarlo desde fuera (UnityEvent, otro script, Animation Event...).</summary>
    public void Trigger()
    {
        if (triggerOnce && _fired) return;
        if (chatApp == null) return;

        _fired = true;

        foreach (var conversation in conversationsToUnlock)
            chatApp.UnlockConversation(conversation);
    }
}
