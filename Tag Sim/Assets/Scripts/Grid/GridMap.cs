using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic Grid class with (0,0) as the bottom-left tile.
/// </summary>
public class GridMap
{
    private int width, height;
    private GridTile[,] tiles;


    /// <summary>
    /// Constructor for creating blank tiles with (0,0) at bottom-left.
    /// </summary>
    public GridMap(int w, int h)
    {
        width = w;
        height = h;

        tiles = new GridTile[w, h];
        for (int i = 0; i < height; i++)  // Iterate rows
        {
            for (int j = 0; j < width; j++)  // Iterate columns
            {
                int adjustedY = height - 1 - i;  // Flip the Y index
                tiles[j, adjustedY] = new GridTile();
                tiles[j, adjustedY].GridCoordinate = new Vector2Int(j, i);  // Assign correct logical coordinates
            }
        }
    }

    /// <summary>
    /// Constructor for setting default tile values with (0,0) at bottom-left.
    /// </summary>
    public GridMap(int w, int h, float value)
    {
        width = w;
        height = h;

        tiles = new GridTile[w, h];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int adjustedY = height - 1 - i;
                tiles[j, adjustedY] = new GridTile();
                tiles[j, adjustedY].GridCoordinate = new Vector2Int(j, i);
                tiles[j, adjustedY].Value = value;
            }
        }
    }

    /// <summary>
    /// Constructor assuming tiles have been initialized elsewhere.
    /// </summary>
    public GridMap(int w, int h, GridTile[,] tiles)
    {
        width = w;
        height = h;
        this.tiles = tiles;
    }

    /// <summary>
    /// Checks if given tile coordinate is within grid.
    /// </summary>
    public bool IsCoordinateWithinGrid(int w, int h)
    {
        return (w >= 0 && w < width) && (h >= 0 && h < height);
    }

    /// <summary>
    /// Returns a reference to the tile at the given grid coordinate, or null if out of bounds.
    /// </summary>
    public GridTile GetTile(int w, int h)
    {
        if (!IsCoordinateWithinGrid(w, h)) return null;
        return tiles[w, h];
    }

    /// <summary>
    /// Sets the value for a tile at a given grid coordinate.
    /// </summary>
    public void SetGridValue(int w, int h, float value)
    {
        if (!IsCoordinateWithinGrid(w, h)) return;
        tiles[w, h].Value = value;
    }

    /// <summary>
    /// Returns the value of a tile at a given grid coordinate.
    /// </summary>
    public float GetGridValue(int w, int h)
    {
        if (!IsCoordinateWithinGrid(w, h)) return -1f;
        return tiles[w, h].Value;
    }

    /// <summary>
    /// Marks a tile as occupied or unoccupied.
    /// </summary>
    public void SetTileOccupied(int x, int y, bool occupied)
    {
        if (IsCoordinateWithinGrid(x, y))
        {
            tiles[x, y].Occupied = occupied;
        }
    }

    /// <summary>
    /// Marks a tile as visible or hidden.
    /// </summary>
    public void SetTileVisible(int x, int y, bool visible)
    {
        if (IsCoordinateWithinGrid(x, y))
        {
            tiles[x, y].Visible = visible;
        }
    }

    /// <summary>
    /// Returns the width and height of the grid.
    /// </summary>
    public (int, int) GetGridSize()
    {
        return (width, height);
    }

    /// <summary>
    /// Normalizes all the values of the grid map, providing an even distribution
    /// </summary>
    public void Normalize()
    {
        // First, find the min and max value of the array.
        float MaxValue = 0;
        float MinValue = 0;

        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                if (tiles[x, y].Value != float.MaxValue)
                {
                    MaxValue = Mathf.Max(MaxValue, tiles[x, y].Value);
                    MinValue = Mathf.Min(MinValue, tiles[x, y].Value);
                }
            }
        }

        // Prevent possible division by zero
        if (MaxValue == MinValue) return;

        // Normalize all numbers
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (tiles[x, y].Value != float.MaxValue)
                {
                    tiles[x, y].Value = (tiles[x, y].Value - MinValue) / (MaxValue - MinValue);
                }
            }
        }
    }
}
