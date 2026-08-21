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
    [Tooltip("Opcional: indicador de mensaje sin leer (punto, badge...). Se activa/desactiva solo.")]
    [SerializeField] private GameObject unreadBadge;

    private ChatConversation _conversation;

    public void Set(ChatConversation conversation, UnityAction onClick)
    {
        _conversation = conversation;
        if (nameLabel) nameLabel.text = conversation.displayName;
        button.onClick.AddListener(onClick);

        conversation.OnUnreadChanged += HandleUnreadChanged;
        RefreshBadge();
    }

    private void OnDestroy()
    {
        if (_conversation != null)
            _conversation.OnUnreadChanged -= HandleUnreadChanged;
    }

    private void HandleUnreadChanged(ChatConversation _) => RefreshBadge();

    private void RefreshBadge()
    {
        if (unreadBadge) unreadBadge.SetActive(_conversation != null && _conversation.HasUnread);
    }
}
