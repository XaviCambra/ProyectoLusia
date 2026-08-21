using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Da de alta contactos y/o conversaciones de chat en runtime (a mitad de
/// partida), reutilizando ChatPhoneApp.RegisterContact/RegisterConversation.
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
    [Serializable]
    private class ConversationEntry
    {
        public ChatContact      contact;
        public ChatConversation conversation;
    }

    [Header("Chat a modificar")]
    [SerializeField] private ChatPhoneApp chatApp;

    [Header("Que dar de alta al dispararse")]
    [Tooltip("Contactos nuevos completos (con todas sus conversaciones).")]
    [SerializeField] private List<ChatContact> contactsToAdd = new();
    [Tooltip("Conversaciones nuevas para contactos que ya existen en el registry.")]
    [SerializeField] private List<ConversationEntry> conversationsToAdd = new();

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

    /// <summary>Ejecuta el alta configurada. Publico para poder llamarlo desde fuera (UnityEvent, otro script, Animation Event...).</summary>
    public void Trigger()
    {
        if (triggerOnce && _fired) return;
        if (chatApp == null) return;

        _fired = true;

        foreach (var contact in contactsToAdd)
            chatApp.RegisterContact(contact);

        foreach (var entry in conversationsToAdd)
            chatApp.RegisterConversation(entry.contact, entry.conversation);
    }
}
