using System.Collections.Generic;
using UnityEngine;

public interface IPathfindingService
{
    List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, int maxDistance);
}
