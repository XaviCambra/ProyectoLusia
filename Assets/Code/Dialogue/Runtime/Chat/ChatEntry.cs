using UnityEngine;

/// <summary>
/// Tipo de contenido de una entrada de chat.
/// Determina qué prefab instancia <see cref="ChatUI"/> y qué campo rellena <see cref="ChatBubble"/>.
/// </summary>
public enum ChatContentType { Text, Emoji, Image }

/// <summary>
/// Entrada inmutable del historial de chat.
/// Usa los métodos de fábrica estáticos para construir cada variante.
/// </summary>
public readonly struct ChatEntry
{
    public readonly ChatContentType contentType;
    public readonly string          speakerName;
    public readonly string          text;
    public readonly bool            isOwn;
    public readonly Sprite          avatarSprite;
    public readonly Sprite          contentSprite; // Emoji o Image; null para Text

    private ChatEntry(ChatContentType contentType, string speakerName, string text,
                      bool isOwn, Sprite avatarSprite, Sprite contentSprite)
    {
        this.contentType   = contentType;
        this.speakerName   = speakerName   ?? string.Empty;
        this.text          = text          ?? string.Empty;
        this.isOwn         = isOwn;
        this.avatarSprite  = avatarSprite;
        this.contentSprite = contentSprite;
    }

    public static ChatEntry ForText(string speakerName, string text,
                                    bool isOwn = false, Sprite avatarSprite = null)
        => new(ChatContentType.Text, speakerName, text, isOwn, avatarSprite, null);

    public static ChatEntry ForEmoji(string speakerName, Sprite emoji,
                                     bool isOwn = false, Sprite avatarSprite = null)
        => new(ChatContentType.Emoji, speakerName, string.Empty, isOwn, avatarSprite, emoji);

    public static ChatEntry ForImage(string speakerName, Sprite image,
                                     bool isOwn = false, Sprite avatarSprite = null)
        => new(ChatContentType.Image, speakerName, string.Empty, isOwn, avatarSprite, image);
}
