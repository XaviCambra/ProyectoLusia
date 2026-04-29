using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbuja individual de chat. Se instancia por cada <see cref="ChatEntry"/>
/// que llega al historial. El prefab decide el aspecto visual (alineación,
/// colores, avatar, etc.); este script asigna textos e imagen de avatar.
/// </summary>
public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text bodyLabel;
    [SerializeField] private Image    avatarImage;

    public void Set(ChatEntry entry)
    {
        if (speakerLabel) speakerLabel.text = entry.speakerName;
        if (bodyLabel)    bodyLabel.text    = entry.text;

        if (avatarImage)
        {
            avatarImage.sprite  = entry.avatarSprite;
            avatarImage.enabled = entry.avatarSprite != null;
        }
    }
}
