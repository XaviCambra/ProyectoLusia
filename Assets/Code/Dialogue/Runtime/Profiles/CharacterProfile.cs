// Runtime/Profiles/CharacterProfile.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Profiles/Character Profile", fileName = "NewCharacterProfile")]
public class CharacterProfile : ScriptableObject
{
    [Header("Datos")]
    public string displayName;

    [Header("Retratos")]
    public List<PortraitEntry> portraits = new();

    public Sprite GetPortraitByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        var p = portraits.Find(x => x != null && x.key == key);
        return p != null ? p.sprite : null;
    }

    public IEnumerable<string> GetPortraitKeys()
    {
        foreach (var p in portraits)
            if (p != null && !string.IsNullOrEmpty(p.key))
                yield return p.key;
    }
}
