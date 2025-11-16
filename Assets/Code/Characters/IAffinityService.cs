public interface IAffinityService
{
    int GetPoints(CharacterDefinition from, CharacterDefinition to);
    void SetPoints(CharacterDefinition from, CharacterDefinition to, int points);
    int AddPoints(CharacterDefinition from, CharacterDefinition to, int delta);
    AffinityBand GetLevel(CharacterDefinition from, CharacterDefinition to);
}
