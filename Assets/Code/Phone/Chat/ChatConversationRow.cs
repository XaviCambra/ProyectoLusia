using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Fila de conversacion dentro de la pantalla de un contacto. Instanciada por ChatConversationListView.
/// </summary>
public class ChatConversationRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Button   button;
    [Tooltip("Verde: hay mensajes nuevos sin leer en esta conversacion.")]
    [SerializeField] private GameObject unreadBadge;
    [Tooltip("Amarillo: el jugador tiene que responder algo pendiente (Choice/ImageChoice). Nunca se muestra a la vez que unreadBadge.")]
    [SerializeField] private GameObject pendingResponseBadge;

    private ChatConversation _conversation;

    public void Set(ChatConversation conversation, UnityAction onClick)
    {
        _conversation = conversation;
        if (nameLabel) nameLabel.text = conversation.displayName;
        button.onClick.AddListener(onClick);

        conversation.OnNotificationChanged += HandleNotificationChanged;
        RefreshBadges();
    }

    private void OnDestroy()
    {
        if (_conversation != null)
            _conversation.OnNotificationChanged -= HandleNotificationChanged;
    }

    private void HandleNotificationChanged(ChatConversation _) => RefreshBadges();

    private void RefreshBadges()
    {
        var notification = _conversation != null ? _conversation.Notification : ChatNotification.None;
        if (unreadBadge)          unreadBadge.SetActive(notification == ChatNotification.Unread);
        if (pendingResponseBadge) pendingResponseBadge.SetActive(notification == ChatNotification.AwaitingResponse);
    }
}
