using System.Collections.Generic;
using UnityEngine;

public class TestCombatHardcode : MonoBehaviour
{
    public List<Character> characterList;

    public CombatManager _CombatManager;

    private void Start()
    {
        _CombatManager.StartCombat(characterList);
    }
}
