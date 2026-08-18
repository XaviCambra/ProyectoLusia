using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escucha GlobalDialogueEvents (el mismo sistema que usa EventDispatcherModule
/// en cualquier dialogo, Retratos o Chat) y desbloquea el ChatContact asociado
/// a la clave del evento. Anadir un contacto desbloqueable nuevo es una fila
/// mas en Rules, sin tocar codigo.
/// </summary>
public class ChatContactUnlockListener : MonoBehaviour
{
    [Serializable]
    private class Rule
    {
        [Tooltip("Debe coincidir con el Event Key del nodo Event que desbloquea este contacto.")]
        public string      eventKey;
        public ChatContact contact;
    }

    [SerializeField] private List<Rule> rules = new();

    private void OnEnable()  => GlobalDialogueEvents.OnNodeEventPayload += OnEvent;
    private void OnDisable() => GlobalDialogueEvents.OnNodeEventPayload -= OnEvent;

    private void OnEvent(DialogueEventPayload payload)
    {
        foreach (var rule in rules)
        {
            if (rule.eventKey != payload.key) continue;

            var wasVisible = rule.contact != null && rule.contact.IsVisible;
            ChatContactUnlocker.Unlock(rule.contact);

            if (rule.contact != null && !wasVisible && rule.contact.IsVisible)
                Debug.Log($"[ChatContactUnlockListener] Contacto desbloqueado: {rule.contact.DisplayName} (evento '{payload.key}')");
        }
    }
}
