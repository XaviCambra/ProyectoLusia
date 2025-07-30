using System.Collections.Generic;
using UnityEngine;

public class GridService : IGridService
{
    private readonly Grid grid;
    private readonly Dictionary<Vector3Int, TileData> tiles;

    public GridService(Grid grid, Dictionary<Vector3Int, TileData> tiles)
    {
        this.grid = grid;
        this.tiles = tiles;
    }

    public TileData GetTileData(Vector3Int cellPosition) => tiles.TryGetValue(cellPosition, out var d) ? d : null;
    public bool IsWalkable(Vector3Int cellPosition) => GetTileData(cellPosition)?.IsWalkable() ?? true;
    public Vector3 GetWorldPosition(Vector3Int cellPosition) => grid.GetCellCenterWorld(cellPosition);
    public Vector3Int WorldToCell(Vector3 worldPosition) => grid.WorldToCell(worldPosition);
}
