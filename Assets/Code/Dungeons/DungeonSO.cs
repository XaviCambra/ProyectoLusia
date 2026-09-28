using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dungeon", menuName = "Exploration/Dungeon/New Dungeon")]
public class DungeonSO : ScriptableObject
{
    public string m_DungeonName;
    enum TDungeonThemes
    {
        Random,
        Egyptian,
        Zombie
    }
    [SerializeField] TDungeonThemes m_DungeonTheme;
    public List<int> m_DialogueNodes = new List<int>();
}
