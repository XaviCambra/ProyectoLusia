using System.Collections.Generic;

public interface ICharacterService
{
    IEnumerable<CharacterDefinition> GetAll();
    CharacterDefinition GetById(string id);
    CharacterDefinition GetByName(string name);
}