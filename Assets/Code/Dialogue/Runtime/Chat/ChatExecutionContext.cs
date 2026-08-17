using System.Collections.Generic;

/// <summary>
/// Estado compartido de una sesión de chat. Se crea por nodo y persiste
/// el perfil activo entre módulos del mismo nodo.
/// </summary>
public sealed class ChatExecutionContext
{
    public CharacterDefinition CurrentProfile     { get; set; }
    public IChatPresenter      Presenter          { get; }
    public IConditionEvaluator ConditionEvaluator { get; }
    public List<ChatEntry>     History            { get; }

    public ChatExecutionContext(
        IChatPresenter      presenter,
        IConditionEvaluator conditionEvaluator,
        List<ChatEntry>     history,
        CharacterDefinition currentProfile)
    {
        Presenter          = presenter;
        ConditionEvaluator = conditionEvaluator;
        History            = history;
        CurrentProfile     = currentProfile;
    }
}
