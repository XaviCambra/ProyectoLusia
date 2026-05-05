using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbuja de "escribiendo..." que vive en el Content del scroll como cualquier mensaje.
/// Instanciada por ChatUI al inicio de ShowTypingAsync y destruida al terminar.
/// </summary>
public sealed class TypingBubble : MonoBehaviour
{
    [SerializeField] private Image    avatarImage;
    [SerializeField] private TMP_Text nameLabel;

    public void Set(CharacterDefinition profile)
    {
        if (avatarImage)
        {
            avatarImage.sprite  = profile?.avatarSprite;
            avatarImage.enabled = profile?.avatarSprite != null;
        }

        if (nameLabel)
            nameLabel.text = profile?.displayName ?? string.Empty;
    }
}
