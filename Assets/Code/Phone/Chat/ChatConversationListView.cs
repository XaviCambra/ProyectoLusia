using System;
using UnityEngine;

/// <summary>
/// Puebla la lista de conversaciones no ocultas de un ChatContact concreto.
/// No decide navegacion: solo notifica que conversacion se ha elegido.
/// </summary>
public class ChatConversationListView : MonoBehaviour
{
    [SerializeField] private Transform           contentParent;
    [SerializeField] private ChatConversationRow rowPrefab;

    public event Action<ChatConversation> OnConversationSelected;

    public void Show(ChatContact contact)
    {
        Clear();
        if (contact == null || contentParent == null || rowPrefab == null) return;

        foreach (var conversation in contact.conversations)
        {
            if (conversation == null || conversation.State == ConversationState.Hidden) continue;

            var row = Instantiate(rowPrefab, contentParent);
            row.Set(conversation, () => OnConversationSelected?.Invoke(conversation));
        }
    }

    public void Clear() => contentParent?.DestroyAllChildren();
}
