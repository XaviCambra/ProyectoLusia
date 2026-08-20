using UnityEngine;
using System.Collections.Generic;

public class DungeonNode : MonoBehaviour
{
    [SerializeField] protected NodeType m_NodeType;
    [SerializeField] bool m_RequiresPosition;

    [SerializeField, Range(0.0f, 100.0f), Tooltip("Valor en % de la dungeon (50 = 50% del progreso)")] int m_Position;

    public List<DungeonNode> m_DungeonNodes = new List<DungeonNode>();

    public virtual void OnActivation() { }
}
