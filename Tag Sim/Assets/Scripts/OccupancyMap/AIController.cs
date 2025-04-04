using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PathfindingComponent), typeof(PerceptionComponent))]
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool chasePlayerWhenSeen = true;
    [SerializeField] private float guessCooldown = 0.75f;
    [SerializeField] private float forgetPlayerDelay = 2f;
    [SerializeField] private float patrolUpdateFrequency = 1.5f;
    [SerializeField] private bool useSmartPrediction = true; // Toggle for the smart prediction behavior

    [Header("Advanced AI Settings")]
    [SerializeField, Range(0.5f, 10f)] private float predictionRadius = 3f; // How far ahead to predict player movement
    [SerializeField, Range(0.7f, 0.99f)] private float diffusionRate = 0.95f; // How quickly suspicion diffuses
    [SerializeField, Range(0.1f, 3f)] private float targetReachedThreshold = 0.5f; // How close AI needs to be to consider target reached
    [SerializeField, Range(0f, 5f)] private float minTargetDwellTime = 1f; // Min time to spend at target before choosing new one

    private PathfindingComponent pathfinding;
    private PerceptionComponent perception;
    private OccupancyMap occupancyMap;
    private GridTile currentTile;
    private GridTile previousTile;
    private GridComponent grid;
    private GridTile currentTargetGuess;

    private float lastGuessTime;
    private float lastSeenPlayerTime = -Mathf.Infinity;
    private float lastPatrolUpdateTime = 0f;
    private float targetReachedTime = 0f; // When we reached the current target
    private bool isChasingPlayer = false;
    private bool isPatrolling = false;
    private bool isWaitingAtTarget = false;

    // For visualization
    private List<GridTile> highlightedTiles = new List<GridTile>();

    private void Start()
    {
        grid = GridComponent.Instance;
        if (grid == null)
        {
            Debug.LogError("AIController: GridComponent.Instance is null.");
            return;
        }

        GridMap gridData = grid.GetGridData();
        if (gridData == null)
        {
            Debug.LogError("AIController: grid.GetGridData() returned null.");
            return;
        }

        pathfinding = GetComponent<PathfindingComponent>();
        perception = GetComponent<PerceptionComponent>();
        if (perception == null)
        {
            Debug.LogError("AIController: PerceptionComponent not found on " + gameObject.name);
            return;
        }

        // Initialize occupancy map
        occupancyMap = new OccupancyMap(gridData);

        // Initialize patrol behavior
        lastGuessTime = Time.time;
        lastPatrolUpdateTime = Time.time;
        TryResumePatrol();
    }

    private void Update()
    {
        UpdateTileOccupation();
        UpdatePerceptionAndOccupancy();
        UpdateBehavior();

        // Check if we've reached our current target
        if (currentTargetGuess != null &&
            Vector2.Distance(transform.position, currentTargetGuess.WorldPosition) < targetReachedThreshold)
        {
            // If we just reached the target, record the time
            if (!isWaitingAtTarget)
            {
                targetReachedTime = Time.time;
                isWaitingAtTarget = true;
                Debug.Log($"Reached target at {currentTargetGuess.GridCoordinate}. Waiting for {minTargetDwellTime} seconds.");
            }
        }
        else
        {
            // Reset waiting state if we moved away from target
            isWaitingAtTarget = false;
        }

        // Regularly update patrol behavior when not chasing
        if (!isChasingPlayer)
        {
            // Choose a new patrol target periodically OR when we've been at our current target long enough
            bool shouldUpdatePatrol = Time.time - lastPatrolUpdateTime > patrolUpdateFrequency;
            bool dwellTimeElapsed = isWaitingAtTarget && Time.time - targetReachedTime > minTargetDwellTime;

            if (shouldUpdatePatrol || dwellTimeElapsed)
            {
                if (dwellTimeElapsed)
                {
                    Debug.Log($"Dwell time elapsed at {currentTargetGuess.GridCoordinate}. Finding new target.");
                }

                TryResumePatrol();
                lastPatrolUpdateTime = Time.time;
                isWaitingAtTarget = false;
            }
        }
    }

    private void UpdateTileOccupation()
    {
        currentTile = grid.GetGridTileAtWorldPosition(transform.position);
        if (currentTile == null) return;

        if (previousTile != null && previousTile != currentTile)
        {
            previousTile.Occupied = false;
        }

        currentTile.Occupied = true;

        
        var gridData = grid.GetGridData();
        if (gridData != null)
        {
            gridData.SetGridValue(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y, 0f);
        }

        previousTile = currentTile;
    }


    private void UpdatePerceptionAndOccupancy()
    {
        // Refresh what AI can see
        perception.UpdatePerception();

        List<GridTile> visibleTiles = perception.GetVisibleTiles();

        if (perception.GetSeenPlayer() != null)
        {
            lastSeenPlayerTime = Time.time;
            isChasingPlayer = true;

            GridTile playerTile = grid.GetGridTileAtWorldPosition(perception.GetSeenPlayer().position);
            if (playerTile != null)
            {
                // When player is seen, clear the map and just mark player's position
                occupancyMap.ClearAndSetSingle(playerTile, perception.GetSeenPlayer());
            }
        }
        else
        {
            // Clear visible tiles first, THEN diffuse
            occupancyMap.ClearVisible(visibleTiles);

            // Only diffuse suspicion when player is not seen
            if (Time.time - lastSeenPlayerTime > 0.5f) // Small delay before starting to guess
            {
                occupancyMap.Diffuse(diffusionRate);
            }
        }
    }

    private void ClearMemoryWhenPlayerSpotted()
    {
        Transform player = perception.GetSeenPlayer();
        if (player != null)
        {
            // When directly seeing the player, clear all previous memory and start fresh
            occupancyMap.ClearRecentMemory();
        }
    }

    private void UpdateBehavior()
    {
        if (!chasePlayerWhenSeen) return;

        Transform player = perception.GetSeenPlayer();
        if (player != null)
        {
            // Direct chase when player is visible
            pathfinding.SetDestination(player.position);
            lastSeenPlayerTime = Time.time;
            isChasingPlayer = true;
            isPatrolling = false;
            isWaitingAtTarget = false;

            // Mark current target as no longer a target
            if (currentTargetGuess != null)
            {
                currentTargetGuess.IsTargetGuess = false;
                currentTargetGuess = null;
            }
        }
        else if (isChasingPlayer)
        {
            // Still chasing but lost sight
            if (Time.time - lastSeenPlayerTime > forgetPlayerDelay)
            {
                Debug.Log("Lost sight of player for " + forgetPlayerDelay + " seconds. Returning to patrol.");
                isChasingPlayer = false;
                isPatrolling = false;
                isWaitingAtTarget = false;
                TryResumePatrol();
            }
            else
            {
                // Continue hunting for player based on last known position and predictions
                if (!isWaitingAtTarget)
                {
                    TrySearchNearLastSeen();
                }
            }
        }
        else if (!isPatrolling && !isWaitingAtTarget)
        {
            // If we're not patrolling and not waiting at a target, find a new patrol target
            TryResumePatrol();
        }
    }

    /// <summary>
    /// Updates the AI's destination to search near the last seen position of the player
    /// </summary>
    private void TrySearchNearLastSeen()
    {
        GridTile bestGuess = occupancyMap.GetMostLikelyTile();
        if (bestGuess == null) return;

        // Don't update target if we're very close to current one
        if (currentTargetGuess != null &&
            Vector2.Distance(currentTargetGuess.WorldPosition, bestGuess.WorldPosition) < 1.0f &&
            Vector2.Distance(transform.position, currentTargetGuess.WorldPosition) < targetReachedThreshold * 2f)
        {
            return; // Keep current target to avoid jitter
        }

        // Clear previous target marker
        if (currentTargetGuess != null)
        {
            currentTargetGuess.IsTargetGuess = false;
        }

        // Mark new target
        currentTargetGuess = bestGuess;
        currentTargetGuess.IsTargetGuess = true;

        // Set destination
        pathfinding.SetDestination(currentTargetGuess.WorldPosition);

        Debug.DrawLine(transform.position, currentTargetGuess.WorldPosition, Color.red, 0.5f);
        Debug.Log($"Searching for player at {currentTargetGuess.GridCoordinate} with value " +
                 $"{grid.GetGridData().GetGridValue(currentTargetGuess.GridCoordinate.x, currentTargetGuess.GridCoordinate.y)}");
    }

    private void TryResumePatrol()
    {
        GridTile bestGuess = occupancyMap.GetMostLikelyTile();
        if (bestGuess == null)
        {
            // Pick a random traversable tile to keep moving
            var (width, height) = grid.GetGridData().GetGridSize();
            List<GridTile> fallback = new();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridTile tile = grid.GetGridData().GetTile(x, y);
                    if (tile != null && tile.Traversable && !tile.Occupied)
                        fallback.Add(tile);
                }
            }

            if (fallback.Count > 0)
            {
                bestGuess = fallback[Random.Range(0, fallback.Count)];
                Debug.Log("TryResumePatrol: No likely tile, defaulting to random fallback.");
            }
            else
            {
                Debug.Log("TryResumePatrol: No fallback available.");
                return;
            }
        }

        // Don't pick the same target or one too close to current position
        if (currentTargetGuess == bestGuess ||
            (Vector2.Distance(transform.position, bestGuess.WorldPosition) < targetReachedThreshold && !isWaitingAtTarget))
        {
            Debug.Log("TryResumePatrol: Selected target is too close or same as current. Picking another.");

            // Try to find a different target
            var (width, height) = grid.GetGridData().GetGridSize();
            List<GridTile> alternatives = new();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridTile tile = grid.GetGridData().GetTile(x, y);
                    float value = grid.GetGridData().GetGridValue(x, y);

                    // Consider tiles with some value and sufficient distance
                    if (tile != null && tile.Traversable && !tile.Occupied &&
                        value > 0.01f && Vector2.Distance(transform.position, tile.WorldPosition) > targetReachedThreshold * 3f)
                    {
                        alternatives.Add(tile);
                    }
                }
            }

            // If we found alternatives, pick one randomly from the top values
            if (alternatives.Count > 0)
            {
                // Sort by value
                alternatives.Sort((a, b) =>
                {
                    float valueA = grid.GetGridData().GetGridValue(a.GridCoordinate.x, a.GridCoordinate.y);
                    float valueB = grid.GetGridData().GetGridValue(b.GridCoordinate.x, b.GridCoordinate.y);
                    return valueB.CompareTo(valueA); // Descending order
                });

                // Take top 20% or at least 1
                int topCount = Mathf.Max(1, alternatives.Count / 5);
                bestGuess = alternatives[Random.Range(0, topCount)];
                Debug.Log($"Selected alternative target from top {topCount} options");
            }
            // If no alternatives, check if we're waiting 
            else if (isWaitingAtTarget && Time.time - targetReachedTime < minTargetDwellTime)
            {
                // Continue waiting
                Debug.Log($"No alternatives found, continuing to wait at current target.");
                return;
            }
            // Last resort: random position far from current
            else
            {
                List<GridTile> farTiles = new();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GridTile tile = grid.GetGridData().GetTile(x, y);
                        if (tile != null && tile.Traversable && !tile.Occupied &&
                            Vector2.Distance(transform.position, tile.WorldPosition) > targetReachedThreshold * 5f)
                        {
                            farTiles.Add(tile);
                        }
                    }
                }

                if (farTiles.Count > 0)
                {
                    bestGuess = farTiles[Random.Range(0, farTiles.Count)];
                    Debug.Log("Selected random distant tile as fallback patrol target");
                }
                else
                {
                    // Truly last resort - force a minimum wait time
                    if (!isWaitingAtTarget)
                    {
                        isWaitingAtTarget = true;
                        targetReachedTime = Time.time;
                        Debug.Log("No suitable patrol targets found. Waiting at current position.");
                    }
                    return;
                }
            }
        }

        // Clear previous target marker
        if (currentTargetGuess != null)
        {
            currentTargetGuess.IsTargetGuess = false;
        }

        // Set new target
        currentTargetGuess = bestGuess;
        currentTargetGuess.IsTargetGuess = true;
        isPatrolling = true;
        isWaitingAtTarget = false;

        Debug.DrawLine(transform.position, currentTargetGuess.WorldPosition, Color.cyan, 2f);
        Debug.Log($"Patrolling to {currentTargetGuess.GridCoordinate} with value {grid.GetGridData().GetGridValue(currentTargetGuess.GridCoordinate.x, currentTargetGuess.GridCoordinate.y)}");

        pathfinding.SetDestination(currentTargetGuess.WorldPosition);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || grid == null) return;

        // Visualize the AI's current mode
        if (isChasingPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }
        else if (isPatrolling)
        {
            Gizmos.color = isWaitingAtTarget ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }

        // Draw line to current target if we have one
        if (currentTargetGuess != null)
        {
            if (isChasingPlayer)
                Gizmos.color = Color.red;
            else if (isWaitingAtTarget)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.cyan;

            Gizmos.DrawLine(transform.position, currentTargetGuess.WorldPosition);

            // Draw target marker
            Gizmos.DrawWireCube(currentTargetGuess.WorldPosition, Vector3.one * 0.5f);
        }

        // Draw debug visualization of the occupancy grid
        if (occupancyMap != null && grid != null)
        {
            var (width, height) = grid.GetGridData().GetGridSize();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = grid.GetGridData().GetGridValue(x, y);
                    if (value > 0.05f)
                    {
                        GridTile tile = grid.GetGridData().GetTile(x, y);
                        if (tile != null)
                        {
                            // Normalized size based on value (0.05f to 1.0f) 
                            float size = Mathf.Lerp(0.1f, 0.4f, Mathf.Clamp01(value));

                            // Color fades from green (low) to red (high)
                            Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(value));
                            Gizmos.DrawCube(tile.WorldPosition, Vector3.one * size);
                        }
                    }
                }
            }
        }
    }
}