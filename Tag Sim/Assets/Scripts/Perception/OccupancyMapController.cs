using UnityEngine;
using System.Collections.Generic;

public class OccupancyMapController : Singleton<OccupancyMapController>
{
    private List<(PerceptionComponent, PathfindingComponent)> perceivers;

    private GridMap occupancyMap;
    private GameObject player;
    private GridComponent grid;

    private GridTile lastKnownPosition;

    // Added for occupancy bark timing control.
    private float occupancyLastBarkTime = 0f;
    public float occupancyBarkInterval = 5f;  // Time (in seconds) between occupancy barks

    private void Start()
    {
        SeedStartingSearchLocation();
    }

    public GridTile GetCurrentTargetState()
    {
        return lastKnownPosition;
    }

    /// <summary>
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
    /// Checks whether our enemies have any information.
    /// </summary>
    private bool IsKnown()
    {
        return lastKnownPosition != null;
    }

    private void OccupancyMapUpdate()
    {
        // STEP 1 & 2: Build a visibility map and clear out cells that are visible to any perceiver.
        (int, int) gridSize = occupancyMap.GetGridSize();
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                if (occupancyMap.GetTile(x, y) != null && occupancyMap.GetTile(x, y).Traversable)
                {
                    foreach (var perceiver in perceivers)
                    {
                        if (perceiver.Item1.HasLOS(occupancyMap.GetTile(x, y)))
                        {
                            occupancyMap.SetGridValue(x, y, 0);
                            break;
                        }
                    }
                }
            }
        }

        // STEP 3: Renormalize the occupancy map.
        occupancyMap.Normalize();

        // STEP 4: Choose the tile with the highest likelihood as the new target.
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
        // Create an empty buffer grid.
        (int, int) gridSize = occupancyMap.GetGridSize();
        GridMap Q = new GridMap(gridSize.Item1, gridSize.Item2, 0);

        // Diffuse probability among the cells.
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                GridTile currentTile = occupancyMap.GetTile(x, y);
                float currentProbability = occupancyMap.GetGridValue(x, y);

                if (currentProbability <= 0.0f) continue;

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
                foreach (GridTile neighbor in neighbors)
                {
                    if (neighbor == null || !neighbor.Traversable) continue;
                    float weight = (Mathf.Abs(neighbor.GridCoordinate.x - x) == 1 && Mathf.Abs(neighbor.GridCoordinate.y - y) == 1)
                                    ? 1.0f / Mathf.Sqrt(2.0f) : 1.0f;
                    validNeighbors.Add((neighbor, weight));
                    totalWeight += weight;
                }

                if (validNeighbors.Count == 0)
                {
                    Q.SetGridValue(x, y, currentProbability);
                    continue;
                }

                float remainingProbability = currentProbability;
                foreach (var neighborPair in validNeighbors)
                {
                    float transferAmount = (currentProbability * neighborPair.Item2) / totalWeight;
                    remainingProbability -= transferAmount;
                    Q.SetGridValue(neighborPair.Item1.GridCoordinate.x, neighborPair.Item1.GridCoordinate.y,
                        Q.GetGridValue(neighborPair.Item1.GridCoordinate.x, neighborPair.Item1.GridCoordinate.y) + transferAmount);
                }
                Q.SetGridValue(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y,
                    Q.GetGridValue(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y) + remainingProbability);
            }
        }

        // Update the main occupancy map grid.
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                occupancyMap.SetGridValue(x, y, Q.GetGridValue(x, y));
            }
        }
    }

    /// <summary>
    /// Assigns each perceiver its current destination.
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
        // Check if any enemy sees the player.
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

        // When the player is visible, set the occupancy map position to the player's tile.
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

            // Trigger occupancy-related bark if enough time has passed.
            if (Time.time - occupancyLastBarkTime >= occupancyBarkInterval)
            {
                foreach (var perceiver in perceivers)
                {
                    AiBarkController barkController = perceiver.Item1.GetComponent<AiBarkController>();
                    if (barkController != null)
                    {
                        barkController.BarkOccupancyUpdate();
                    }
                }
                occupancyLastBarkTime = Time.time;
            }
        }

        // Diffuse the occupancy map if we have a valid known location.
        if (IsKnown())
        {
            OccupancyMapDiffuse();
        }

        // Destination setting is now managed by the spatial component.
        // SetPerceiverDestinations();
    }
}
