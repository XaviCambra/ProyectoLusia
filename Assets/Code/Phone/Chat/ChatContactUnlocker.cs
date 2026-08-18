/// <summary>
/// Punto de entrada unico para desbloquear un ChatContact desde cualquier
/// sistema del juego. Desbloquea su primera conversacion oculta (basta con
/// una para que ChatContact.IsVisible pase a true).
/// </summary>
public static class ChatContactUnlocker
{
    public static void Unlock(ChatContact contact)
    {
        if (contact == null) return;

        var conversation = contact.conversations.Find(c => c != null && c.State == ConversationState.Hidden);
        conversation?.Unlock();
    }
}
