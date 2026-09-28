using System.Collections.Generic;
using UnityEngine;

public class NodeManager : MonoBehaviour
{
    int m_CurrentStoryLine;

    List<List<DungeonNode>> m_NodeGenerationList = new List<List<DungeonNode>>();

    [SerializeField] private int TEST_DialogueNodes;


    public void GenerateDungeon()
    {
        // Esto es simplemente para pillar cuantos nodos hay que generar
        int _DungeonLevels = TEST_DialogueNodes--;

        /*
         * Asi generamos la dungeon
         * Por cada nivel de la dungeon, se generan entre 1 y 3 nodos
         * Se añade cada nivel de forma independiente pero estan sin conectar
         */
        for (int i = 0; i < _DungeonLevels; i++)
        {
            int _Level = Random.Range(1, 4);
            List<DungeonNode> dungeonNodes = new List<DungeonNode>();
            for(int j = 0; j < _Level; j++)
            {
                DungeonNode dungeonNode = new DungeonNode();
                dungeonNode.GenerateNode();
            }
            m_NodeGenerationList.Add(dungeonNodes);
            dungeonNodes.Clear();
        }

        List<DungeonNode> dungeonNodes2 = new List<DungeonNode>();
        /*
         * Aqui meteremos primero los nodos de
         */
        m_NodeGenerationList.Insert(0, dungeonNodes2);
        m_NodeGenerationList.Insert(m_NodeGenerationList.Count, dungeonNodes2);
    }
}
