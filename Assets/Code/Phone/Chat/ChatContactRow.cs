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

    public void Set(ChatContact contact, UnityAction onClick)
    {
        if (nameLabel) nameLabel.text = contact.DisplayName;
        if (avatarImage && contact.DisplayImage) avatarImage.sprite = contact.DisplayImage;
        button.onClick.AddListener(onClick);
    }
}
