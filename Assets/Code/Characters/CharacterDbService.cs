using System.Collections.Generic;

public sealed class CharacterDbService : ICharacterService
{
    private readonly CharacterDatabase db;
    public CharacterDbService(CharacterDatabase db) => this.db = db;

    public IEnumerable<CharacterDefinition> GetAll() => db.All;
    public CharacterDefinition GetById(string id) => db.GetById(id);
    public CharacterDefinition GetByName(string name) => db.GetByName(name);
}