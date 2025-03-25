using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic Grid class we can use for pathfinding, spacial functions, and o-map (hopefully.)
/// </summary>
public class GridMap
{
    private int width, height;
    private GridTile[,] tiles;

    /// <summary>
    /// Constructor for creating blank new tiles to fill the grid.
    /// </summary>
    public GridMap(int w, int h)
    {
        width = w;
        height = h;

        tiles = new GridTile[w, h];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                tiles[j, i] = new GridTile();
                tiles[j, i].GridCoordinate = new Vector2Int(j, i);
            }
        }
    }

    /// <summary>
    /// Constructor for creating blank new tiles to fill the grid and set to given default value.
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
                tiles[j, i] = new GridTile();
                tiles[j, i].GridCoordinate = new Vector2Int(j, i);
                tiles[j, i].Value = value;
            }
        }
    }

    /// <summary>
    /// Constructor which assumes tiles have been initialized elsewhere.
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
    /// Returns a reference to the tile at the given grid coordinate, or null if not found.
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
}
