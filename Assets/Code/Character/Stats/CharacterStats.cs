using System.Collections.Generic;

public class CharacterStats
{
    private HashSet<TileEffect> immunities = new();

    public void AddImmunity(TileEffect effect) => immunities.Add(effect);
    public void RemoveImmunity(TileEffect effect) => immunities.Remove(effect);

    public bool IsImmuneTo(TileEffect effect) => immunities.Contains(effect);

    // Luego añadir más propiedades como equipo, buffs, velocidad, etc.
}
