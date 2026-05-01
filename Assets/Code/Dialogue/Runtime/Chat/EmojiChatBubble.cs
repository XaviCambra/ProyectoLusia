using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbuja de emoji: muestra avatar + nombre del hablante igual que una burbuja de texto,
/// pero en lugar del globo blanco con texto muestra el sprite directamente.
/// Se instancia por cada <see cref="EmojiModule"/> procesado por el ChatRunner.
/// </summary>
public class EmojiChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private Image    avatarImage;
    [SerializeField] private Image    emojiImage;

    public void Set(Sprite emoji, string speakerName, Sprite avatar)
    {
        if (speakerLabel)
            speakerLabel.text = speakerName ?? string.Empty;

        if (avatarImage)
        {
            avatarImage.sprite  = avatar;
            avatarImage.enabled = avatar != null;
        }

        if (emojiImage)
        {
            emojiImage.sprite         = emoji;
            emojiImage.preserveAspect = true;
        }
    }
}
