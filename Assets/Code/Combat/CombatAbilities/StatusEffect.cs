public abstract class StatusEffect
{
    public int m_Duration;

    public virtual void OnApply(CombatCharacter _Owner) { }
    public virtual void OnTurnStart(CombatCharacter owner) { }
    public virtual void OnTurnEnd(CombatCharacter owner) { }
    public virtual void OnRemove(CombatCharacter owner) { }
}