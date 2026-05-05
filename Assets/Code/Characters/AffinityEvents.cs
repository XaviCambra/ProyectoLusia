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

public sealed class AffinityLevelChangedArgs
{
    public CharacterDefinition From      { get; }
    public CharacterDefinition To        { get; }
    public AffinityBand        OldLevel  { get; }
    public AffinityBand        NewLevel  { get; }

    public AffinityLevelChangedArgs(CharacterDefinition from, CharacterDefinition to, AffinityBand oldLevel, AffinityBand newLevel)
    {
        From     = from;
        To       = to;
        OldLevel = oldLevel;
        NewLevel = newLevel;
    }
}
