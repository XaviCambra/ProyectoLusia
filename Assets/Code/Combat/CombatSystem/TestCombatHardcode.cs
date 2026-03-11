using System.Collections.Generic;
using UnityEngine;

public class TestCombatHardcode : MonoBehaviour
{
    public List<Character> characterList;

    public CombatOrderManager orderManager;

    private void Start()
    {
        foreach (Character character in characterList)
        {
            orderManager.AddCharacter(character);
        }
    }
}
