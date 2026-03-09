using TMPro;
using UnityEngine;

/// <summary>
/// Burbuja individual de chat. Se instancia por cada <see cref="ChatEntry"/>
/// que llega al historial. El prefab decide el aspecto visual (alineación,
/// colores, avatar, etc.); este script solo asigna los textos.
/// </summary>
public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text bodyLabel;

    public void Set(ChatEntry entry)
    {
        if (speakerLabel) speakerLabel.text = entry.speakerName;
        if (bodyLabel)    bodyLabel.text    = entry.text;
    }
}
