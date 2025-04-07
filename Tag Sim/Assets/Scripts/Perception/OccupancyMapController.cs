using UnityEngine;
using System.Collections.Generic;

public class OccupancyMapController : Singleton<OccupancyMapController>
{
    private List<(PerceptionComponent, PathfindingComponent)> perceivers;

    private GridMap occupancyMap;
    private GameObject player;
    private GridComponent grid;

    private GridTile lastKnownPosition;

    private void Start()
    {
        SeedStartingSearchLocation();
    }

    public GridTile GetCurrentTargetState()
    {
        return lastKnownPosition;
    }

    /// </summary>
    /// Find the closest perceiver to the given tile, not including the tile itself.
    /// </summary>
    public float GetDistanceToClosestPerceiver(GridTile tile)
    {
        float closestDistance = float.MaxValue;
        foreach (var perceiver in perceivers)
        {
            if (perceiver.Item1 == this) continue; // Skip self
            float distance = grid.DistanceBetweenTiles(tile, grid.GetGridTileAtWorldPosition(perceiver.Item1.transform.position));
            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }
        return closestDistance;
    }

    public void SeedStartingSearchLocation()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        occupancyMap = GridComponent.Instance.GetGridData();
        grid = GridComponent.Instance;

        lastKnownPosition = grid.GetGridTileAtWorldPosition(player.transform.position);
        lastKnownPosition.test = true;
        lastKnownPosition.Value = 1f;
    }

    /// <summary>
    /// Registers a 'perceiver' enemy with this component for coordination and sharing of occupancy map.
    /// </summary>
    public void RegisterPerceiver(PerceptionComponent perceptionComponent, PathfindingComponent pathfindingComponent)
    {
        if (perceivers == null) perceivers = new List<(PerceptionComponent, PathfindingComponent)>();
        perceivers.Add((perceptionComponent, pathfindingComponent));
    }

    /// <summary>
    /// Clears out all the values in our occupancy map except a given tile, which is set to 1.
    /// </summary>
    private void OccupancyMapSetPosition(GridTile tile)
    {
        (int, int) gridSize = occupancyMap.GetGridSize();
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                if (tile.GridCoordinate == new Vector2Int(x, y))
                {
                    occupancyMap.SetGridValue(x, y, 1);
                }
                else
                {
                    occupancyMap.SetGridValue(x, y, 0);
                }
            }
        }
    }

    /// <summary>
    /// Do our enemies have any information.
    /// </summary>
    /// <returns></returns>
    private bool IsKnown()
    {
        return lastKnownPosition != null;
    }

    private void OccupancyMapUpdate()
    {
        // STEP 1: Build a visibility map, based on the perception components of the AIs in the world
        // The visibility map is a simple map where each cell is either 0 (not currently visible to ANY perceiver) or 1
        // (currently visible to one or more perceivers).
        // STEP 2: Clear out the probability in the visible cells
        (int, int) gridSize = occupancyMap.GetGridSize();
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                // Only evaluate valid and traversable tiles.
                if (occupancyMap.GetTile(x, y) != null && occupancyMap.GetTile(x, y).Traversable)
                {
                    foreach (var perceiver in perceivers)
                    {
                        // Since we know at this point none of our visible cells contain the player,
                        // we can safely elimanate all possibility from this tile.
                        if (perceiver.Item1.HasLOS(occupancyMap.GetTile(x, y)))
                        {
                            occupancyMap.SetGridValue(x, y, 0);
                            break;
                        }
                    }
                }
            }
        }

        // STEP 3: Renormalize the OMap, so that it's still a valid probability distribution
        occupancyMap.Normalize();

        // STEP 4: Extract the highest-likelihood cell on the omap and refresh the lastKnownPosition.
        GridTile highestValue = occupancyMap.GetTile(0, 0);
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                if (occupancyMap.GetTile(x, y).Value > highestValue.Value)
                {
                    highestValue = occupancyMap.GetTile(x, y);
                }
            }
        }
        lastKnownPosition.test = false;
        lastKnownPosition = highestValue;
        lastKnownPosition.test = true;
    }

    private void OccupancyMapDiffuse()
    {
        // Create our empty buffer
        (int, int) gridSize = occupancyMap.GetGridSize();
        GridMap Q = new GridMap(gridSize.Item1, gridSize.Item2, 0);

        // Iterate over occupancy map
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                GridTile currentTile = occupancyMap.GetTile(x, y);
                float currentProbability = occupancyMap.GetGridValue(x, y);

                // Skip cells with no probability
                if (currentProbability <= 0.0f) continue;

                // List of neighbors
                List<GridTile> neighbors = new List<GridTile>()
                {
                    grid.GetTile(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y + 1),
                    grid.GetTile(currentTile.GridCoordinate.x + 1, currentTile.GridCoordinate.y + 1),
                    grid.GetTile(currentTile.GridCoordinate.x + 1, currentTile.GridCoordinate.y),
                    grid.GetTile(currentTile.GridCoordinate.x + 1, currentTile.GridCoordinate.y - 1),
                    grid.GetTile(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y - 1),
                    grid.GetTile(currentTile.GridCoordinate.x - 1, currentTile.GridCoordinate.y - 1),
                    grid.GetTile(currentTile.GridCoordinate.x - 1, currentTile.GridCoordinate.y),
                    grid.GetTile(currentTile.GridCoordinate.x - 1, currentTile.GridCoordinate.y + 1),
                };

                float totalWeight = 0.0f;
                List<(GridTile, float)> validNeighbors = new List<(GridTile, float)>();
                // Collect valid neighbors and compute weights
                foreach (GridTile neighbor in neighbors)
                {
                    // Only valid neighbors that are traversable are eligible for diffusion
                    if (neighbor == null) continue;
                    if (!neighbor.Traversable) continue;

                    // Determine diffusion 'weight' based on diagonals
                    float weight = (Mathf.Abs(neighbor.GridCoordinate.x - x) == 1 && Mathf.Abs(neighbor.GridCoordinate.y - y) == 1)
                                       ? 1.0f / Mathf.Sqrt(2.0f) : 1.0f;
                    validNeighbors.Add((neighbor, weight));
                    totalWeight += weight;
                }

                // If no valid neighbors don't change probability
                if (validNeighbors.Count == 0)
                {
                    Q.SetGridValue(x, y, currentProbability);
                    continue;
                }

                // Spread probability among valid neighbors, accounting for diagonals
                float remainingProbability = currentProbability;
                foreach (var neighborPair in validNeighbors)
                {
                    float transferAmount = (currentProbability * neighborPair.Item2) / totalWeight;
                    remainingProbability -= transferAmount;

                    Q.SetGridValue(neighborPair.Item1.GridCoordinate.x, neighborPair.Item1.GridCoordinate.y,
                        Q.GetGridValue(neighborPair.Item1.GridCoordinate.x, neighborPair.Item1.GridCoordinate.y) + transferAmount);
                }

                // Keep the remaining probability in the original cell	
                Q.SetGridValue(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y,
                        Q.GetGridValue(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y) + remainingProbability);
            }
        }

        // Update our occupancy map (and the original grid)
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                occupancyMap.SetGridValue(x, y, Q.GetGridValue(x, y));
            }
        }
    }

    /// <summary>
    /// Assigns the perceivers (enemies) their current destinations.
    /// </summary>
    private void SetPerceiverDestinations()
    {
        if (!IsKnown()) return;
        foreach (var perceiver in perceivers)
        {
            perceiver.Item2.SetDestination(lastKnownPosition.WorldPosition);
        }
    }

    public void Update()
    {
        // Check if any of our enemies can see the player right now.
        bool playerVisible = false;
        foreach (var perceiver in perceivers)
        {
            if (perceiver.Item1.HasLOS(grid.GetGridTileAtWorldPosition(player.transform.position)))
            {
                perceiver.Item1.playerInSight = true;
                playerVisible = true;
                break;
            }
            else
            {
                perceiver.Item1.playerInSight = false;
            }
        }

        // If they can, all enemies should move towards the player.
        if (playerVisible)
        {
            lastKnownPosition.test = false;
            lastKnownPosition = grid.GetGridTileAtWorldPosition(player.transform.position);
            lastKnownPosition.test = true;
            OccupancyMapSetPosition(lastKnownPosition);
        }
        else
        {
            OccupancyMapUpdate();
        }

        // As long as I'm known, whether I'm immediate or not, diffuse the probability in the omap
        if (IsKnown())
        {
            OccupancyMapDiffuse();
        }

        // Move our enemies.
        //SetPerceiverDestinations(); This should now be handled by the spatial component
    }
}
