using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PathfindingComponent), typeof(PerceptionComponent))]
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool chasePlayerWhenSeen = true;

    private PathfindingComponent pathfinding;
    private PerceptionComponent perception;
    private OccupancyMap occupancyMap;
    private GridTile currentTile;
    private GridTile previousTile;
    private GridComponent grid;
    private GridTile currentTargetGuess;
    private float guessCooldown = 0.75f;
    private float lastGuessTime;

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

        occupancyMap = new OccupancyMap(gridData);
    }

    private void Update()
    {
        UpdateTileOccupation();
        UpdatePerceptionAndOccupancy();
        if (Time.time - lastGuessTime > guessCooldown)
        {
            GuessPlayerLocation();
            lastGuessTime = Time.time;
        }
        UpdateBehavior();
    }

    /// <summary>
    /// Updates which tile the AI is currently occupying.
    /// </summary>
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

    /// <summary>
    /// Updates what the AI can see and feeds that data into the occupancy map.
    /// </summary>
    private void UpdatePerceptionAndOccupancy()
    {
        List<GridTile> visibleTiles = perception.GetVisibleTiles();
        occupancyMap.UpdateVisibility(visibleTiles);
    }


    private void GuessPlayerLocation()
    {
        Debug.Log("Guessing where the Player is");
        GridMap grid = GridComponent.Instance.GetGridData();
        var (width, height) = grid.GetGridSize();

        GridTile bestGuess = null;
        float longestUnseen = -1f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridTile tile = grid.GetTile(x, y);
                if (tile == null || !tile.Traversable || tile.Occupied || tile.Visible)
                    continue;

                float unseenTime = Time.time - tile.LastSeenTime;
                if (unseenTime > longestUnseen)
                {
                    longestUnseen = unseenTime;
                    bestGuess = tile;
                }
            }
        }

        if (currentTargetGuess != null)
            currentTargetGuess.IsTargetGuess = false;

        currentTargetGuess = bestGuess;

        if (currentTargetGuess != null)
        {
            if (bestGuess == null)
            {
                Debug.Log("Oops No GUESS.");
                return;
            }

            currentTargetGuess.IsTargetGuess = true;

            // Optional: Move toward it
            GetComponent<PathfindingComponent>().SetDestination(currentTargetGuess.WorldPosition);
        }
    }

    /// <summary>
    /// if the player is detected, update the pathfinding destination.
    /// </summary>
    private void UpdateBehavior()
    {
        if (!chasePlayerWhenSeen) return;

        Transform player = perception.GetSeenPlayer();
        if (player != null)
        {
            pathfinding.SetDestination(player.position);
        }
    }
}
