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

    public void Set(ChatConversation conversation, UnityAction onClick)
    {
        if (nameLabel) nameLabel.text = conversation.displayName;
        button.onClick.AddListener(onClick);
    }
}
