using System;
using System.Collections.Generic;
using UnityEngine;

public enum ConversationState { Hidden, Active, Finished }

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

    // Historial completo y marca de "no leido" de la sesion actual. Igual que
    // _state, es estado de sesion no serializado: se reconstruye mientras la
    // conversacion corre (en pantalla o en segundo plano), no persiste entre partidas.
    [NonSerialized] private List<ChatEntry> _history = new();
    [NonSerialized] private bool _hasUnread;

    public IReadOnlyList<ChatEntry> History => _history;
    public bool HasUnread => _hasUnread;

    public event Action<ChatConversation> OnUnlocked;
    public event Action<ChatConversation> OnUnreadChanged;

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
        OnUnreadChanged?.Invoke(this);
    }

    public void MarkRead()
    {
        if (!_hasUnread) return;
        _hasUnread = false;
        OnUnreadChanged?.Invoke(this);
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
