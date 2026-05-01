using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbuja individual de chat. Se instancia por cada <see cref="ChatEntry"/>
/// que llega al historial. El prefab decide el aspecto visual (fondo, alineación,
/// avatar, etc.); este script asigna textos, imagen de avatar y contenido sprite.
///
/// Campos opcionales según el tipo de prefab:
///   - Texto : bodyLabel    asignado, contentImage null
///   - Emoji : contentImage asignado, bodyLabel    null
///   - Imagen: contentImage asignado, bodyLabel    null (con fondo en el prefab)
/// </summary>
public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text bodyLabel;
    [SerializeField] private Image    avatarImage;
    [SerializeField] private Image    contentImage; // para Emoji e Image

    public void Set(ChatEntry entry)
    {
        if (speakerLabel) speakerLabel.text = entry.speakerName;

        if (avatarImage)
        {
            avatarImage.sprite  = entry.avatarSprite;
            avatarImage.enabled = entry.avatarSprite != null;
        }

        if (entry.contentType == ChatContentType.Text)
        {
            if (bodyLabel) bodyLabel.text = entry.text;
        }
        else
        {
            if (contentImage)
            {
                contentImage.sprite         = entry.contentSprite;
                contentImage.preserveAspect = true;
            }
        }
    }
}
