using System.Collections.Generic;
using UnityEngine;

public class PathfinderService : IPathfindingService
{
    private readonly IGridService gridService;

    public PathfinderService(IGridService gridService)
    {
        this.gridService = gridService;
    }

    private class Node
    {
        public Vector3Int Position;
        public Node Parent;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;

        public Node(Vector3Int pos, Node parent, int gCost, int hCost)
        {
            Position = pos;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
        }
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, int maxDistance)
    {
        var openSet = new List<Node>();
        var closedSet = new HashSet<Vector3Int>();

        openSet.Add(new Node(start, null, 0, Heuristic(start, goal)));

        while (openSet.Count > 0)
        {
            // Nodo con menor FCost
            Node current = openSet[0];
            foreach (var node in openSet)
            {
                if (node.FCost < current.FCost || (node.FCost == current.FCost && node.HCost < current.HCost))
                    current = node;
            }

            openSet.Remove(current);
            closedSet.Add(current.Position);

            if (current.Position == goal && current.GCost <= maxDistance)
                return RetracePath(current);

            foreach (var neighbor in GetNeighbors(current.Position))
            {
                if (closedSet.Contains(neighbor) || !gridService.IsWalkable(neighbor))
                    continue;

                int tentativeGCost = current.GCost + 1;

                Node existingNode = openSet.Find(n => n.Position == neighbor);
                if (existingNode == null)
                {
                    if (tentativeGCost > maxDistance) continue;

                    Node newNode = new Node(neighbor, current, tentativeGCost, Heuristic(neighbor, goal));
                    openSet.Add(newNode);
                }
                else if (tentativeGCost < existingNode.GCost)
                {
                    existingNode.GCost = tentativeGCost;
                    existingNode.Parent = current;
                }
            }
        }

        return null; // No hay camino válido
    }

    private int Heuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            cell + Vector3Int.up,
            cell + Vector3Int.down,
            cell + Vector3Int.left,
            cell + Vector3Int.right
        };
        return neighbors;
    }

    private List<Vector3Int> RetracePath(Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }
}
