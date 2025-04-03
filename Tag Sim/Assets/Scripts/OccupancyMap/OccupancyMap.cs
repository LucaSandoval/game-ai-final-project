using System.Collections.Generic;
using UnityEngine;

public class OccupancyMap
{
    private GridMap grid;
    private Dictionary<Vector2Int, bool> occupiedTiles = new Dictionary<Vector2Int, bool>();

    public OccupancyMap(GridMap grid)
    {
        this.grid = grid;
        SeedRandomOccupancy(); // Add initial random occupancy to start patrolling
    }

    /// <summary>
    /// Seeds the occupancy map with some initial random values to kickstart patrolling
    /// </summary>
    private void SeedRandomOccupancy()
    {
        var (width, height) = grid.GetGridSize();
        List<GridTile> validTiles = new List<GridTile>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridTile tile = grid.GetTile(x, y);
                if (tile != null && tile.Traversable && !tile.Occupied)
                    validTiles.Add(tile);
            }
        }

        if (validTiles.Count > 0)
        {
            // Pick a random tile and set its value
            GridTile seedTile = validTiles[Random.Range(0, validTiles.Count)];
            grid.SetGridValue(seedTile.GridCoordinate.x, seedTile.GridCoordinate.y, 1f);
            Debug.Log($"Initial seed at {seedTile.GridCoordinate} with value 1.0");
        }
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
        // Clear all visibility first
        var (width, height) = grid.GetGridSize();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridTile tile = grid.GetTile(x, y);
                if (tile != null)
                    tile.Visible = false;
            }
        }

        // Set visibility for current frame
        foreach (GridTile tile in visibleTiles)
        {
            tile.Visible = true;
        }
    }

    /// <summary>
    /// Diffuses the probability values across the grid to simulate AI searching nearby areas
    /// </summary>
    public void Diffuse(float decayFactor = 0.95f)
    {
        // Create a temporary copy of the grid for reading values while we update
        float[,] tempValues = new float[grid.GetGridSize().Item1, grid.GetGridSize().Item2];
        var (width, height) = grid.GetGridSize();

        // First, copy all current values to our temp array
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tempValues[x, y] = grid.GetGridValue(x, y);
            }
        }

        // Then do the diffusion using the temp values, storing results in the original grid
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = tempValues[x, y];
                if (value <= 0f) continue;

                GridTile currentTile = grid.GetTile(x, y);
                if (currentTile == null || !currentTile.Traversable) continue;

                // Apply decay
                value *= decayFactor;

                // Get all 8 neighbors
                List<Vector2Int> neighbors = new List<Vector2Int>
                {
                    new Vector2Int(x, y + 1),    // North
                    new Vector2Int(x + 1, y + 1), // Northeast
                    new Vector2Int(x + 1, y),    // East
                    new Vector2Int(x + 1, y - 1), // Southeast
                    new Vector2Int(x, y - 1),    // South
                    new Vector2Int(x - 1, y - 1), // Southwest
                    new Vector2Int(x - 1, y),    // West
                    new Vector2Int(x - 1, y + 1)  // Northwest
                };

                float totalWeight = 0f;
                List<(Vector2Int pos, float weight)> validNeighbors = new();

                // Find valid neighboring tiles and assign weights
                foreach (var n in neighbors)
                {
                    // Skip if out of bounds
                    if (!grid.IsCoordinateWithinGrid(n.x, n.y)) continue;

                    GridTile neighborTile = grid.GetTile(n.x, n.y);
                    if (neighborTile != null && neighborTile.Traversable)
                    {
                        // Diagonal neighbors get a reduced weight
                        float weight = (n.x != x && n.y != y) ? 1f / Mathf.Sqrt(2f) : 1f;
                        validNeighbors.Add((n, weight));
                        totalWeight += weight;
                    }
                }

                // Nothing to diffuse to
                if (validNeighbors.Count == 0 || totalWeight <= 0f) continue;

                // Update the current cell's value in the grid (keeping some value in the center)
                grid.SetGridValue(x, y, value * 0.3f); // Keep 30% of the value in the current cell

                // Distribute the remaining 70% to neighbors
                float valueToDistribute = value * 0.7f;
                foreach (var (pos, weight) in validNeighbors)
                {
                    float transfer = (valueToDistribute * weight) / totalWeight;
                    float current = grid.GetGridValue(pos.x, pos.y);
                    grid.SetGridValue(pos.x, pos.y, current + transfer);
                }
            }
        }

        // After diffusion, check if there are any significant values left
        bool hasValues = false;
        float highestValue = 0f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = grid.GetGridValue(x, y);
                if (value > 0.01f) // Small threshold to determine if there are significant values
                {
                    hasValues = true;
                    if (value > highestValue)
                        highestValue = value;
                }
            }
        }

        // If no significant values remain, seed a new random point to encourage patrolling
        if (!hasValues || highestValue < 0.1f)
        {
            Debug.Log("No significant values remain in occupancy map. Seeding new patrol point.");
            SeedRandomOccupancy();
        }
    }

    /// <summary>
    /// Clears the entire grid and sets a single tile to maximum value
    /// </summary>
    public void ClearAndSetSingle(GridTile tile)
    {
        for (int y = 0; y < grid.GetGridSize().Item2; y++)
        {
            for (int x = 0; x < grid.GetGridSize().Item1; x++)
            {
                grid.SetGridValue(x, y, 0f);
            }
        }

        if (tile != null)
        {
            grid.SetGridValue(tile.GridCoordinate.x, tile.GridCoordinate.y, 1f);
            Debug.Log($"Set tile at {tile.GridCoordinate} to 1.0 (player location)");
        }
    }

    /// <summary>
    /// Clears just the visible tiles (setting their values to 0)
    /// </summary>
    public void ClearVisible(List<GridTile> visibleTiles)
    {
        // Only set visible tiles to 0 (we've confirmed player isn't there)
        foreach (var tile in visibleTiles)
        {
            if (tile != null)
            {
                grid.SetGridValue(tile.GridCoordinate.x, tile.GridCoordinate.y, 0f);
            }
        }

        // After clearing visible tiles, check if any values remain
        bool hasValues = false;
        var (width, height) = grid.GetGridSize();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid.GetGridValue(x, y) > 0.01f)
                {
                    hasValues = true;
                    break;
                }
            }
            if (hasValues) break;
        }

        // If no values remain, seed a new random value
        if (!hasValues)
        {
            Debug.Log("No values remain after clearing visible tiles. Seeding new patrol point.");
            SeedRandomOccupancy();
        }
    }

    /// <summary>
    /// Returns the tile with the highest probability value
    /// </summary>
    public GridTile GetMostLikelyTile()
    {
        GridTile best = null;
        float highest = 0.01f; // Minimum threshold to consider a tile

        var (width, height) = grid.GetGridSize();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridTile tile = grid.GetTile(x, y);
                if (tile == null || !tile.Traversable) continue;

                float value = grid.GetGridValue(x, y);

                if (value > highest)
                {
                    highest = value;
                    best = tile;
                }
            }
        }

        // If no tile has a value above our threshold, seed a new random point
        if (best == null)
        {
            Debug.Log("No suitable patrol target found. Seeding new random target.");
            SeedRandomOccupancy();
            return GetMostLikelyTile(); // Try again
        }

        return best;
    }
}