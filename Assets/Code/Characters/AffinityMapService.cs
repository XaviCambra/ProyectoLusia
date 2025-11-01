public sealed class AffinityMapService : IAffinityService
{
    private readonly CharacterAffinityMap map;
    public AffinityMapService(CharacterAffinityMap map) => this.map = map;

    public int GetPoints(CharacterDefinition from, CharacterDefinition to) => map.GetPoints(from, to);
    public void SetPoints(CharacterDefinition from, CharacterDefinition to, int points) => map.SetPoints(from, to, points);
    public int AddPoints(CharacterDefinition from, CharacterDefinition to, int delta) => map.AddPoints(from, to, delta);
    public AffinityBand GetLevel(CharacterDefinition from, CharacterDefinition to) => map.GetLevel(from, to);
}