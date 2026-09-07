public sealed class AffinityChangedArgs
{
    public CharacterDefinition From       { get; }
    public CharacterDefinition To         { get; }
    public int                 OldPoints  { get; }
    public int                 NewPoints  { get; }

    public AffinityChangedArgs(CharacterDefinition from, CharacterDefinition to, int oldPoints, int newPoints)
    {
        From      = from;
        To        = to;
        OldPoints = oldPoints;
        NewPoints = newPoints;
    }
}

public sealed class AffinityRelationshipChangedArgs
{
    public CharacterDefinition From      { get; }
    public CharacterDefinition To        { get; }
    public AffinityRelationship        OldRelationship  { get; }
    public AffinityRelationship        NewRelationship  { get; }

    public AffinityRelationshipChangedArgs(CharacterDefinition from, CharacterDefinition to, AffinityRelationship oldLevel, AffinityRelationship newLevel)
    {
        From     = from;
        To       = to;
        OldRelationship = oldLevel;
        NewRelationship = newLevel;
    }
}

public sealed class AffinityKnownChangedArgs
{
    public CharacterDefinition From  { get; }
    public CharacterDefinition To    { get; }
    public bool                Known { get; }

    public AffinityKnownChangedArgs(CharacterDefinition from, CharacterDefinition to, bool known)
    {
        From  = from;
        To    = to;
        Known = known;
    }
}
