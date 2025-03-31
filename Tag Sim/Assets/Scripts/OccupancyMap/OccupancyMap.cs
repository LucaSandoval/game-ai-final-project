using System.Collections.Generic;
using UnityEngine;

public class OccupancyMap
{
    private GridMap grid;
    private Dictionary<Vector2Int, bool> occupiedTiles = new Dictionary<Vector2Int, bool>();

    public OccupancyMap(GridMap grid)
    {
        this.grid = grid;
    }

    /// <summary>
    /// Marks a tile as occupied or free.
    /// </summary>
    public void SetTileOccupied(Vector2Int tileCoord, bool occupied)
    {
        if (grid.IsCoordinateWithinGrid(tileCoord.x, tileCoord.y))
        {
            grid.GetTile(tileCoord.x, tileCoord.y).Occupied = occupied;
            occupiedTiles[tileCoord] = occupied;
        }
    }

    /// <summary>
    /// Checks if a tile is occupied.
    /// </summary>
    public bool IsTileOccupied(Vector2Int tileCoord)
    {
        return occupiedTiles.ContainsKey(tileCoord) && occupiedTiles[tileCoord];
    }

    /// <summary>
    /// Updates visibility based on AI perception.
    /// </summary>
    public void UpdateVisibility(List<GridTile> visibleTiles)
    {
        foreach (GridTile tile in visibleTiles)
        {
            tile.Visible = true;
        }
    }
}
