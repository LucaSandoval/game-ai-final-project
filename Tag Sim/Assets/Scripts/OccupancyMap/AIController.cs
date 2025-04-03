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
            if (Time.time - lastPatrolUpdateTime > patrolUpdateFrequency)
            {
                TryResumePatrol();
                lastPatrolUpdateTime = Time.time;
            }

            // If reached our target or don't have one, get a new one
            if (currentTargetGuess != null &&
                Vector2.Distance(transform.position, currentTargetGuess.WorldPosition) < 0.5f)
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
        if (perception.GetSeenPlayer() != null)
        {
            lastSeenPlayerTime = Time.time;

            //Do you ever chase the player
            if (!isChasingPlayer)
            {
                Debug.Log("Player spotted! Beginning chase.");
            }

            isChasingPlayer = true;
            isPatrolling = false;

            // Set player's position as the only occupancy value
            GridTile playerTile = grid.GetGridTileAtWorldPosition(perception.GetSeenPlayer().position);
            if (playerTile != null)
            {
                occupancyMap.ClearAndSetSingle(playerTile);
            }
        }
        else
        {
            // Player not seen, update occupancy map
            occupancyMap.ClearVisible(perception.GetVisibleTiles());

            // Only diffuse if we're not directly chasing the player
            if (!isChasingPlayer)
            {
                occupancyMap.Diffuse(0.95f);
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
                // Continue moving toward last known position (handled by pathfinding)
                // The occupancy map will handle the diffusion of the last known position
            }
        }
        else if (!isPatrolling ||
                (currentTargetGuess != null && Vector2.Distance(transform.position, currentTargetGuess.WorldPosition) < 0.5f))
        {
            TryResumePatrol();
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
}