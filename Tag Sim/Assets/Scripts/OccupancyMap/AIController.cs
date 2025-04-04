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
    private bool isChasingPlayer = false;
    private bool isPatrolling = false;

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

        // Regularly update patrol behavior when not chasing
        if (!isChasingPlayer)
        {
            // Periodically check for a new patrol target
            if (Time.time - lastPatrolUpdateTime > patrolUpdateFrequency)
            {
                TryResumePatrol();
                lastPatrolUpdateTime = Time.time;
            }

            // If we've reached our target or don't have one, get a new one
            if (currentTargetGuess != null &&
                Vector2.Distance(transform.position, currentTargetGuess.WorldPosition) < targetReachedThreshold)
            {
                Debug.Log("Reached patrol target. Finding a new one.");
                TryResumePatrol();
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
        previousTile = currentTile;
    }

    private void UpdatePerceptionAndOccupancy()
    {
        Transform playerTransform = perception.GetSeenPlayer();

        if (playerTransform != null)
        {
            lastSeenPlayerTime = Time.time;

            // Only announce when transitioning from not chasing to chasing
            if (!isChasingPlayer)
            {
                Debug.Log("Player spotted! Beginning chase.");
            }

            isChasingPlayer = true;
            isPatrolling = false;

            // Set player's position as the only occupancy value
            GridTile playerTile = grid.GetGridTileAtWorldPosition(playerTransform.position);
            if (playerTile != null)
            {
                // Pass the player transform for velocity tracking
                occupancyMap.ClearAndSetSingle(playerTile, playerTransform);
            }
        }
        else
        {
            // Player not seen, update occupancy map
            occupancyMap.ClearVisible(perception.GetVisibleTiles());

            // Only diffuse if we're not directly chasing the player
            if (!isChasingPlayer || Time.time - lastSeenPlayerTime > 0.5f)
            {
                occupancyMap.Diffuse(diffusionRate);
            }
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
                TryResumePatrol();
            }
            else
            {
                // Continue hunting for player based on last known position and predictions
                TrySearchNearLastSeen();
            }
        }
        else if (!isPatrolling ||
                (currentTargetGuess != null && Vector2.Distance(transform.position, currentTargetGuess.WorldPosition) < targetReachedThreshold))
        {
            // If we're not patrolling or have reached our target, find a new patrol target
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

        if (currentTargetGuess != bestGuess)
        {
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
    }

    private void TryResumePatrol()
    {
        GridTile bestGuess = occupancyMap.GetMostLikelyTile();
        if (bestGuess == null)
        {
            Debug.Log("TryResumePatrol: No guess available, will try again soon.");
            return;
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

        Debug.DrawLine(transform.position, currentTargetGuess.WorldPosition, Color.cyan, 2f);
        Debug.Log($"Patrolling to {currentTargetGuess.GridCoordinate} with value {grid.GetGridData().GetGridValue(currentTargetGuess.GridCoordinate.x, currentTargetGuess.GridCoordinate.y)}");

        pathfinding.SetDestination(currentTargetGuess.WorldPosition);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || occupancyMap == null || grid == null) return;

        // Visualize the AI's current mode
        if (isChasingPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }
        else if (isPatrolling)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }

        // Draw line to current target if we have one
        if (currentTargetGuess != null)
        {
            if (isChasingPlayer)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.cyan;

            Gizmos.DrawLine(transform.position, currentTargetGuess.WorldPosition);

            // Draw target marker
            Gizmos.DrawWireCube(currentTargetGuess.WorldPosition, Vector3.one * 0.5f);
        }
    }
}