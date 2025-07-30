using UnityEngine;

public interface IGridService
{
    TileData GetTileData(Vector3Int cellPosition);
    bool IsWalkable(Vector3Int cellPosition);
    Vector3 GetWorldPosition(Vector3Int cellPosition);
    Vector3Int WorldToCell(Vector3 worldPosition);
}