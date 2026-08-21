using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Fila de contacto en la lista de contactos del chat. Instanciada por ChatContactListView.
/// </summary>
public class ChatContactRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image    avatarImage;
    [SerializeField] private Button   button;
    [Tooltip("Verde: hay mensajes nuevos sin leer en alguna conversacion del contacto.")]
    [SerializeField] private GameObject unreadBadge;
    [Tooltip("Amarillo: el jugador tiene que responder algo pendiente en alguna conversacion del contacto. Nunca se muestra a la vez que unreadBadge.")]
    [SerializeField] private GameObject pendingResponseBadge;

    private ChatContact _contact;

    public void Set(ChatContact contact, UnityAction onClick)
    {
        _contact = contact;
        if (nameLabel) nameLabel.text = contact.DisplayName;
        if (avatarImage && contact.DisplayImage) avatarImage.sprite = contact.DisplayImage;
        button.onClick.AddListener(onClick);

        foreach (var conversation in contact.conversations)
            if (conversation != null)
                conversation.OnNotificationChanged += HandleNotificationChanged;

        RefreshBadges();
    }

    private void OnDestroy()
    {
        if (_contact == null) return;
        foreach (var conversation in _contact.conversations)
            if (conversation != null)
                conversation.OnNotificationChanged -= HandleNotificationChanged;
    }

    private void HandleNotificationChanged(ChatConversation _) => RefreshBadges();

    private void RefreshBadges()
    {
        var notification = _contact != null ? _contact.Notification : ChatNotification.None;
        if (unreadBadge)          unreadBadge.SetActive(notification == ChatNotification.Unread);
        if (pendingResponseBadge) pendingResponseBadge.SetActive(notification == ChatNotification.AwaitingResponse);
    }
}
