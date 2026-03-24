using System.Collections.Generic;
using UnityEngine;

public class TestCombatHardcode : MonoBehaviour
{
    public List<Character> characterList;

    public CombatManager orderManager;

    private void Start()
    {
        orderManager.StartCombat(characterList);
    }
}
