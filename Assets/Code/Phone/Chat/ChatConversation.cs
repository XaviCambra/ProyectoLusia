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

    public event Action<ChatConversation> OnUnlocked;

    private void OnEnable()
    {
        bool allMet = conditions.Count == 0 || conditions.TrueForAll(c => c.value);
        _state = allMet ? ConversationState.Active : ConversationState.Hidden;
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
