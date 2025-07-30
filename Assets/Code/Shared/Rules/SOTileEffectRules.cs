using UnityEngine;

[CreateAssetMenu(menuName = "GameRules/SOTileEffectRules")]
public class SOTileEffectRules : ScriptableObject
{
    [System.Serializable]
    public struct EffectCost
    {
        public TileEffect effect;
        public int movementCost;
    }

    public EffectCost[] effectCosts;

    public int GetCost(TileEffect effect)
    {
        foreach (var ec in effectCosts)
            if (ec.effect == effect)
                return ec.movementCost;
        return 0;
    }
}
