using System.Collections.Generic;
using UnityEngine;

public class OccupancyMap
{
    private GridMap grid;
    private Dictionary<Vector2Int, bool> occupiedTiles = new Dictionary<Vector2Int, bool>();

    // Player prediction variables
    private Vector2 lastPlayerPosition;
    private Vector2 playerVelocity;
    private float lastPlayerSpottedTime = -100f;
    private List<Vector2Int> playerHistoryPositions = new List<Vector2Int>();
    private const int MAX_HISTORY_SIZE = 10; // Store the last 10 positions

    private HashSet<Vector2Int> recentlyCheckedTiles = new HashSet<Vector2Int>();
    private float memoryDuration = 5f; // How long to remember checked tiles (in seconds)
    private Dictionary<Vector2Int, float> tileLastCheckedTime = new Dictionary<Vector2Int, float>();

    // Diffusion control
    private float minDiffusionValue = 0.01f; // Minimum value to consider during diffusion
    private float minSeedValue = 0.2f; // Minimum value when seeding a new point

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
            grid.SetGridValue(seedTile.GridCoordinate.x, seedTile.GridCoordinate.y, minSeedValue);
            Debug.Log($"Initial seed at {seedTile.GridCoordinate} with value {minSeedValue}");

            // Add a few more seeds with lower values to create interesting patrol patterns
            for (int i = 0; i < 3; i++)
            {
                GridTile additionalSeed = validTiles[Random.Range(0, validTiles.Count)];
                float seedValue = minSeedValue * (0.5f - (i * 0.1f)); // Decreasing values
                grid.SetGridValue(additionalSeed.GridCoordinate.x, additionalSeed.GridCoordinate.y, seedValue);
            }
        }
    }

    /// <summary>
    /// Marks a tile as recently checked, to prevent backtracking
    /// </summary>
    private void MarkTileAsChecked(Vector2Int tileCoord)
    {
        recentlyCheckedTiles.Add(tileCoord);
        tileLastCheckedTime[tileCoord] = Time.time;
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
    /// Clears old memory of checked tiles
    /// </summary>
    private void UpdateMemory()
    {
        // Remove tiles from memory if they were checked too long ago
        List<Vector2Int> tilesToRemove = new List<Vector2Int>();

        foreach (var tile in recentlyCheckedTiles)
        {
            if (tileLastCheckedTime.TryGetValue(tile, out float lastCheckedTime))
            {
                if (Time.time - lastCheckedTime > memoryDuration)
                {
                    tilesToRemove.Add(tile);
                }
            }
            else
            {
                // If no timestamp, remove it
                tilesToRemove.Add(tile);
            }
        }

        foreach (var tile in tilesToRemove)
        {
            recentlyCheckedTiles.Remove(tile);
            tileLastCheckedTime.Remove(tile);
        }
    }

    /// <summary>
    /// Updates the AI's knowledge about the player's position and velocity
    /// </summary>
    public void UpdatePlayerKnowledge(Transform player, GridTile playerTile)
    {
        if (player == null || playerTile == null) return;

        Vector2 currentPlayerPos = player.position;
        float timeSinceLastSpotted = Time.time - lastPlayerSpottedTime;

        // Calculate player velocity if we've seen them before
        if (lastPlayerSpottedTime > 0 && timeSinceLastSpotted < 1.0f)
        {
            Vector2 newVelocity = (currentPlayerPos - lastPlayerPosition) / timeSinceLastSpotted;

            // Smooth velocity calculation (70% new, 30% old)
            playerVelocity = Vector2.Lerp(playerVelocity, newVelocity, 0.7f);

            Debug.Log($"Updated player velocity: {playerVelocity.x:F2}, {playerVelocity.y:F2}, magnitude: {playerVelocity.magnitude:F2}");
        }

        // Add to position history
        Vector2Int gridPos = playerTile.GridCoordinate;

        // Only add if it's a new position
        if (playerHistoryPositions.Count == 0 || playerHistoryPositions[0] != gridPos)
        {
            playerHistoryPositions.Insert(0, gridPos);

            // Trim history if too long
            if (playerHistoryPositions.Count > MAX_HISTORY_SIZE)
            {
                playerHistoryPositions.RemoveAt(playerHistoryPositions.Count - 1);
            }

            Debug.Log($"Added player position to history: {gridPos}. History size: {playerHistoryPositions.Count}");
        }

        lastPlayerPosition = currentPlayerPos;
        lastPlayerSpottedTime = Time.time;
    }

    /// <summary>
    /// Predicts where the player might be based on last known position and velocity
    /// </summary>
    private List<Vector2Int> PredictPlayerMovement(float timeSinceLastSeen)
    {
        List<Vector2Int> predictedPositions = new List<Vector2Int>();

        // If we've never seen the player or it's been too long, return empty list
        if (lastPlayerSpottedTime < 0 || playerHistoryPositions.Count == 0 || timeSinceLastSeen > 5.0f)
        {
            return predictedPositions;
        }

        // Base prediction on last known position
        Vector2Int lastKnownPos = playerHistoryPositions[0];

        // Use player velocity to predict future positions
        // Estimate how far they could have gone in the time since last seen
        float distanceCovered = playerVelocity.magnitude * timeSinceLastSeen;
        int estimatedTiles = Mathf.CeilToInt(distanceCovered);

        // Limit prediction distance, but ensure at least 1 if there's any velocity
        estimatedTiles = playerVelocity.magnitude > 0.1f ?
                         Mathf.Clamp(estimatedTiles, 2, 15) : 0;  //Change the length of the guesses and where they are going

        Debug.Log($"Player prediction: velocity mag {playerVelocity.magnitude:F2}, " +
                 $"time since seen {timeSinceLastSeen:F2}, estimated tiles: {estimatedTiles}");

        // Get player direction from velocity
        Vector2 normalizedDir = playerVelocity.magnitude > 0.1f ?
                               playerVelocity.normalized : Vector2.zero;

        // Add the primary prediction based on velocity
        for (int i = 1; i <= estimatedTiles; i++)
        {
            // Calculate predicted position based on velocity
            Vector2 predictedWorldPos = lastPlayerPosition + (normalizedDir * i);

            // Convert to grid position
            GridTile predictedTile = FindNearestTraversableTile(predictedWorldPos);
            if (predictedTile != null)
            {
                predictedPositions.Add(predictedTile.GridCoordinate);

                // Give higher weight to closer predictions
                float weight = 1.0f - ((float)i / estimatedTiles);
                float predictionValue = Mathf.Lerp(0.3f, 0.8f, weight);

                // Add the prediction with diminishing weight based on distance
                grid.SetGridValue(predictedTile.GridCoordinate.x, predictedTile.GridCoordinate.y,
                                  Mathf.Max(grid.GetGridValue(predictedTile.GridCoordinate.x, predictedTile.GridCoordinate.y),
                                  predictionValue));

                Debug.Log($"Added prediction at {predictedTile.GridCoordinate} with value {predictionValue:F2}");
            }
        }

        // If very little time has passed since we've seen the player, make the last known position very important
        if (timeSinceLastSeen < 1.0f)
        {
            Vector2Int lastPosCoord = playerHistoryPositions[0];
            float lastKnownValue = Mathf.Lerp(1.0f, 0.6f, timeSinceLastSeen); // Decays from 1.0 to 0.6 over 1 second

            if (grid.IsCoordinateWithinGrid(lastPosCoord.x, lastPosCoord.y))
            {
                grid.SetGridValue(lastPosCoord.x, lastPosCoord.y, lastKnownValue);
                Debug.Log($"Last known position at {lastPosCoord} set to high value: {lastKnownValue:F2}");
            }
        }

        // Additionally, check places the player has been before
        if (playerHistoryPositions.Count > 1)
        {
            for (int i = 1; i < playerHistoryPositions.Count; i++)
            {
                Vector2Int historyPos = playerHistoryPositions[i];

                // Add a small weight to historical positions, diminishing with age
                float historyWeight = 0.2f * (1.0f - ((float)i / playerHistoryPositions.Count));

                if (grid.IsCoordinateWithinGrid(historyPos.x, historyPos.y))
                {
                    float currentValue = grid.GetGridValue(historyPos.x, historyPos.y);
                    grid.SetGridValue(historyPos.x, historyPos.y, Mathf.Max(currentValue, historyWeight));

                    predictedPositions.Add(historyPos);
                }
            }
        }

        return predictedPositions;
    }

    /// <summary>
    /// Finds the nearest traversable tile to a world position
    /// </summary>
    private GridTile FindNearestTraversableTile(Vector2 worldPosition)
    {
        // First try direct conversion
        GridTile tile = null;

        // This depends on your GridComponent implementation for converting world to grid
        // You might need to adjust this based on your actual grid system
        if (GridComponent.Instance != null)
        {
            tile = GridComponent.Instance.GetGridTileAtWorldPosition(worldPosition);

            // If valid, return it
            if (tile != null && tile.Traversable)
            {
                return tile;
            }
        }

        // Otherwise, search nearby tiles
        var (width, height) = grid.GetGridSize();
        float closestDistance = float.MaxValue;
        GridTile closestTile = null;

        // Search in a limited radius to keep performance reasonable
        const int SEARCH_RADIUS = 5;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridTile candidateTile = grid.GetTile(x, y);
                if (candidateTile != null && candidateTile.Traversable)
                {
                    float distance = Vector2.Distance(worldPosition, candidateTile.WorldPosition);
                    if (distance < closestDistance && distance < SEARCH_RADIUS)
                    {
                        closestDistance = distance;
                        closestTile = candidateTile;
                    }
                }
            }
        }

        return closestTile;
    }

    /// <summary>
    /// Diffuses the probability values across the grid to simulate AI searching nearby areas
    /// </summary>
    public void Diffuse(float decayFactor = 0.95f)
    {
        // Update memory of recently checked areas
        UpdateMemory();

        // Special case: If we've seen the player recently, add predictions BEFORE diffusion
        float timeSinceLastSeen = Time.time - lastPlayerSpottedTime;
        PredictPlayerMovement(timeSinceLastSeen);

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
        int cellsWithValue = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = tempValues[x, y];
                if (value <= minDiffusionValue) continue; // Skip very small values for efficiency

                cellsWithValue++;
                GridTile currentTile = grid.GetTile(x, y);
                if (currentTile == null || !currentTile.Traversable) continue;

                // Completely clear suspicion for visible tiles
                if (currentTile.Visible)
                {
                    grid.SetGridValue(x, y, 0f);
                    continue;
                }

                // Apply a stronger decay to recently checked tiles
                if (recentlyCheckedTiles.Contains(currentTile.GridCoordinate))
                {
                    // More aggressive decay for recently checked areas
                    value *= (decayFactor * 0.5f);

                    // If the value gets too small, just clear it completely
                    if (value < 0.05f)
                    {
                        grid.SetGridValue(x, y, 0f);
                        continue;
                    }
                }
                else
                {
                    // Normal decay for other tiles
                    value *= decayFactor;
                }

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
                        // Skip visible neighboring tiles
                        if (neighborTile.Visible) continue;

                        // Reduce diffusion to recently checked tiles
                        float weight = (n.x != x && n.y != y) ? 1f / Mathf.Sqrt(2f) : 1f;

                        // Reduce weight for recently checked areas
                        if (recentlyCheckedTiles.Contains(n))
                        {
                            weight *= 0.3f; // Significantly reduce diffusion into already-checked areas
                        }

                        // If player was last seen moving, bias diffusion in that direction
                        if (playerVelocity.magnitude > 0.1f && timeSinceLastSeen < 3.0f)
                        {
                            // Calculate direction from current tile to neighbor
                            Vector2 dirToNeighbor = (neighborTile.WorldPosition - currentTile.WorldPosition).normalized;

                            // Dot product to see how aligned this direction is with player velocity
                            float alignment = Vector2.Dot(dirToNeighbor, playerVelocity.normalized);

                            // Add a bonus to weights in the player's travel direction
                            if (alignment > 0)
                            {
                                weight *= (1.0f + alignment * 0.5f);
                            }
                        }

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

        // After diffusion, check if any significant values are left
        bool hasValues = cellsWithValue > 0;
        float highestValue = 0f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = grid.GetGridValue(x, y);
                if (value > minDiffusionValue)
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
            Debug.Log("No significant values remain in occupancy map. Seeding new patrol points.");
            SeedRandomOccupancy();
        }
    }

    /// <summary>
    /// Clears the entire grid and sets a single tile to maximum value
    /// </summary>
    public void ClearAndSetSingle(GridTile tile, Transform player = null)
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

            // If player transform was provided, update our knowledge of the player
            if (player != null)
            {
                UpdatePlayerKnowledge(player, tile);
            }
        }
    }

    public void ClearRecentMemory()
    {
        recentlyCheckedTiles.Clear();
        tileLastCheckedTime.Clear();
        Debug.Log("Cleared AI memory of recently checked areas - player spotted");
    }

    /// <summary>
    /// Clears just the visible tiles (setting their values to 0)
    /// </summary>
    public void ClearVisible(List<GridTile> visibleTiles)
    {
        // Update memory before processing new visible tiles
        UpdateMemory();

        // Only set visible tiles to 0 (we've confirmed player isn't there)
        int clearedCount = 0;

        foreach (var tile in visibleTiles)
        {
            if (tile != null)
            {
                // Only count it as cleared if it had a value
                if (grid.GetGridValue(tile.GridCoordinate.x, tile.GridCoordinate.y) > minDiffusionValue)
                {
                    clearedCount++;
                }

                // Set the value to 0 for all visible tiles
                grid.SetGridValue(tile.GridCoordinate.x, tile.GridCoordinate.y, 0f);
                // Mark the tile as recently seen
                tile.LastSeenTime = Time.time;
                // Add to recently checked memory
                MarkTileAsChecked(tile.GridCoordinate);
            }
        }

        if (clearedCount > 0)
        {
            Debug.Log($"Cleared {clearedCount} visible tiles (confirmed player not there)");
        }
    }

    public void SeedRandomPatrolIfNeeded()
    {
        bool hasValues = false;
        var (width, height) = grid.GetGridSize();

        for (int y = 0; y < height && !hasValues; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid.GetGridValue(x, y) > minDiffusionValue)
                {
                    hasValues = true;
                    break;
                }
            }
        }

        if (!hasValues)
        {
            Debug.Log("OccupancyMap: No values left. Seeding fallback patrol point.");
            SeedRandomOccupancy();
        }
    }


    /// <summary>
    /// Returns the tile with the highest probability value
    /// </summary>
    public GridTile GetMostLikelyTile()
    {
        GridTile best = null;
        float highest = minDiffusionValue; // Minimum threshold to consider a tile

        var (width, height) = grid.GetGridSize();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridTile tile = grid.GetTile(x, y);
                if (tile == null || !tile.Traversable) continue;

                // Skip recently checked tiles to prevent backtracking
                if (recentlyCheckedTiles.Contains(tile.GridCoordinate)) continue;

                float value = grid.GetGridValue(x, y);

                if (value > highest)
                {
                    highest = value;
                    best = tile;
                }
            }
        }

        // If no high-value unchecked tile was found, look for any tile
        if (best == null)
        {
            // First try finding a tile that has SOME value, even if recently checked
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridTile tile = grid.GetTile(x, y);
                    if (tile == null || !tile.Traversable) continue;

                    float value = grid.GetGridValue(x, y);
                    if (value > highest && value > 0.05f)
                    {
                        highest = value;
                        best = tile;
                    }
                }
            }

            // If still nothing, seed a new random point
            if (best == null)
            {
                Debug.Log("No suitable patrol target found. Seeding new random target.");
                SeedRandomOccupancy();
                return GetMostLikelyTile(); // Try again
            }
        }

        return best;
    }
}