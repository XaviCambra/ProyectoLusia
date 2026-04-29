using UnityEngine;

/// <summary>
/// Entrada inmutable del historial de chat.
/// Contiene el nombre del hablante, el texto y el avatar tal como aparecen en el grafo.
/// </summary>
public readonly struct ChatEntry
{
    public readonly string speakerName;
    public readonly string text;
    public readonly bool   isOwn;
    public readonly Sprite avatarSprite;

    public ChatEntry(string speakerName, string text, bool isOwn = false, Sprite avatarSprite = null)
    {
        this.speakerName  = speakerName  ?? string.Empty;
        this.text         = text         ?? string.Empty;
        this.isOwn        = isOwn;
        this.avatarSprite = avatarSprite;
    }
}
