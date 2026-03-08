public interface IConditionEvaluator
{
    /// <summary>
    /// Devuelve true si la opción cumple todas las condiciones activas (afinidad, progreso, etc.).
    /// </summary>
    bool IsAllowed(ChoiceModule.ChoiceData choice);
}
