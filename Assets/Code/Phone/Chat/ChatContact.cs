using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contacto del móvil. Puede ser individual (un CharacterDefinition)
/// o grupo (nombre + imagen). Agrupa sus ChatConversation en orden.
/// </summary>
[CreateAssetMenu(fileName = "ChatContact", menuName = "Chat/Chat Contact")]
public class ChatContact : ScriptableObject
{
    public bool isGroup;

    [Tooltip("Solo si no es grupo.")]
    public CharacterDefinition character;

    [Tooltip("Solo si es grupo.")]
    public string groupName;
    [Tooltip("Solo si es grupo.")]
    public Sprite groupImage;

    public List<ChatConversation> conversations = new();

    public string DisplayName  => isGroup ? groupName          : character?.displayName ?? "?";
    public Sprite DisplayImage => isGroup ? groupImage         : character?.avatarSprite;
    public bool   IsVisible    => conversations.Exists(c => c.State != ConversationState.Hidden);
    public bool   HasNew       => conversations.Exists(c => c.State == ConversationState.Active);
}
