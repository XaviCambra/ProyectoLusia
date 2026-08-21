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
    [Tooltip("Opcional: indicador de mensaje sin leer en alguna conversacion del contacto.")]
    [SerializeField] private GameObject unreadBadge;

    public void Set(ChatContact contact, UnityAction onClick)
    {
        if (nameLabel) nameLabel.text = contact.DisplayName;
        if (avatarImage && contact.DisplayImage) avatarImage.sprite = contact.DisplayImage;
        if (unreadBadge) unreadBadge.SetActive(contact.HasUnread);
        button.onClick.AddListener(onClick);
    }
}
