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
    [Tooltip("Valor inicial al empezar la partida. En runtime NO se modifica (ver ChatConversation.SetCondition).")]
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
    [Tooltip("Condiciones de desbloqueo con su valor INICIAL (dato de autoria). El valor en runtime vive aparte y no toca el asset.")]
    public List<NamedBool> conditions = new();

    // Valor actual de cada condicion durante la partida. Separado de 'conditions' para
    // que el progreso nunca se escriba en el asset (en editor, cambiar un campo
    // serializado de un ScriptableObject en Play Mode se queda en el asset al salir).
    [NonSerialized] private Dictionary<string, bool> _conditionValues = new();

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
        _conditionValues = new Dictionary<string, bool>();
        foreach (var c in conditions)
            if (c != null && !string.IsNullOrEmpty(c.name))
                _conditionValues[c.name] = c.value;

        _state = AllConditionsMet() ? ConversationState.Active : ConversationState.Hidden;
    }

    private bool AllConditionsMet()
    {
        foreach (var met in _conditionValues.Values)
            if (!met) return false;
        return true;
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

    /// <summary>Cambia el valor en runtime de una condicion (no el asset) y desbloquea la
    /// conversacion si con eso se cumplen todas.</summary>
    public void SetCondition(string conditionName, bool value)
    {
        if (string.IsNullOrEmpty(conditionName) || !_conditionValues.TryGetValue(conditionName, out var current))
        {
            Debug.LogWarning($"[ChatConversation] '{name}' no tiene ninguna condicion llamada '{conditionName}'.", this);
            return;
        }
        if (current == value) return;
        _conditionValues[conditionName] = value;
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
        if (AllConditionsMet())
        {
            _state = ConversationState.Active;
            OnUnlocked?.Invoke(this);
        }
    }
}
