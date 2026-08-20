using UnityEngine;
using System.Collections.Generic;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] List<DungeonNode> m_DungeonNodes = new List<DungeonNode>();
    [SerializeField] List<DungeonNode> m_GeneratedDungeon = new List<DungeonNode>();
    [SerializeField, Range(5.0f, 99.0f)] int m_MinNodes;
    [SerializeField, Range(5.0f, 99.0f)] int m_MaxNodes;

    void GenerateDungeon()
    {
        m_MaxNodes = Mathf.Max(m_MaxNodes, m_MinNodes);
        List<DungeonNode> _NewDungeon = new List<DungeonNode>();


        _NewDungeon.Reverse();
        m_GeneratedDungeon = _NewDungeon;
    }



    void NodeNavigation()
    {

    }
}

public class DungeonNodeSpawn
{
    public DungeonNode DungeonNode;

    [Range(0.0f, 100.0f)] public int m_SpawnRate;
}