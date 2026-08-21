using System;
using System.Collections.Generic;
using UnityEngine;

public enum ConversationState { Hidden, Active, Finished }

/// <summary>
/// Estado de notificacion de una conversacion (o de un contacto, agregando las suyas).
/// Son mutuamente excluyentes por diseno: AwaitingResponse siempre gana sobre Unread,
/// asi que solo se pinta un icono a la vez (ver ChatConversation.Notification).
/// </summary>
public enum ChatNotification { None, Unread, AwaitingResponse }

[Serializable]
public class NamedBool
{
    public string name;
    public bool   value;
}

/// <summary>
/// Un bloque de diálogo dentro de un ChatContact.
/// Pasa de Hidden → Active cuando todas las condiciones son true.
/// Pasa a Finished cuando el diálogo termina de reproducirse.
/// </summary>
[CreateAssetMenu(fileName = "ChatConversation", menuName = "Chat/Chat Conversation")]
public class ChatConversation : ScriptableObject
{
    public string          displayName;
    public DialogueGraph   graph;
    public List<NamedBool> conditions = new();

    [NonSerialized] private ConversationState _state;
    public ConversationState State => _state;

    // Historial completo y notificacion de la sesion actual. Igual que _state, es
    // estado de sesion no serializado: se reconstruye mientras la conversacion
    // corre (en pantalla o en segundo plano), no persiste entre partidas.
    [NonSerialized] private List<ChatEntry> _history = new();
    [NonSerialized] private bool _hasUnread;
    [NonSerialized] private bool _awaitingResponse;

    public IReadOnlyList<ChatEntry> History => _history;
    public bool HasUnread => _hasUnread;
    public bool AwaitingResponse => _awaitingResponse;

    /// <summary>Que icono le corresponde a esta conversacion ahora mismo. AwaitingResponse tiene prioridad sobre Unread.</summary>
    public ChatNotification Notification =>
        _awaitingResponse ? ChatNotification.AwaitingResponse :
        _hasUnread        ? ChatNotification.Unread :
        ChatNotification.None;

    public event Action<ChatConversation> OnUnlocked;
    public event Action<ChatConversation> OnNotificationChanged;

    private void OnEnable()
    {
        bool allMet = conditions.Count == 0 || conditions.TrueForAll(c => c.value);
        _state = allMet ? ConversationState.Active : ConversationState.Hidden;
    }

    /// <summary>Añade un mensaje al historial visible de esta conversacion (ver ConversationChatPresenter).</summary>
    public void AppendHistory(ChatEntry entry) => _history.Add(entry);

    public void MarkUnread()
    {
        if (_hasUnread) return;
        _hasUnread = true;
        OnNotificationChanged?.Invoke(this);
    }

    public void MarkRead()
    {
        if (!_hasUnread) return;
        _hasUnread = false;
        OnNotificationChanged?.Invoke(this);
    }

    /// <summary>El jugador tiene un Choice/ImageChoice pendiente de responder en esta conversacion.</summary>
    public void MarkAwaitingResponse()
    {
        if (_awaitingResponse) return;
        _awaitingResponse = true;
        OnNotificationChanged?.Invoke(this);
    }

    /// <summary>El Choice/ImageChoice pendiente ya se respondio (o se cancelo del todo).</summary>
    public void MarkResponded()
    {
        if (!_awaitingResponse) return;
        _awaitingResponse = false;
        OnNotificationChanged?.Invoke(this);
    }

    public void SetCondition(string conditionName, bool value)
    {
        var cond = conditions.Find(c => c.name == conditionName);
        if (cond == null || cond.value == value) return;
        cond.value = value;
        TryUnlock();
    }

    public void MarkFinished()
    {
        if (_state == ConversationState.Active)
            _state = ConversationState.Finished;
    }

    /// <summary>Fuerza el paso a Active ignorando las condiciones. Para desbloqueos directos/scripted.</summary>
    public void Unlock()
    {
        if (_state != ConversationState.Hidden) return;
        _state = ConversationState.Active;
        OnUnlocked?.Invoke(this);
    }

    private void TryUnlock()
    {
        if (_state != ConversationState.Hidden) return;
        if (conditions.TrueForAll(c => c.value))
        {
            _state = ConversationState.Active;
            OnUnlocked?.Invoke(this);
        }
    }
}
