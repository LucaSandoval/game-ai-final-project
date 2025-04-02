using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PathfindingComponent), typeof(PerceptionComponent))]
public class AIController : MonoBehaviour
{
    [Header("AI Behavior Settings")]
    [SerializeField] private bool chasePlayerWhenSeen = true;

    private PathfindingComponent pathfinding;
    private PerceptionComponent perception;
    private OccupancyMap occupancyMap;
    private GridTile currentTile;
    private GridTile previousTile;
    private GridComponent grid;

    private void Awake()
    {
        grid = GridComponent.Instance;
        pathfinding = GetComponent<PathfindingComponent>();
        perception = GetComponent<PerceptionComponent>();
        occupancyMap = new OccupancyMap(grid.GetGridData());
    }

    private void Update()
    {
        UpdateTileOccupation();
        UpdatePerceptionAndOccupancy();
        //UpdateBehavior();
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

    ///// <summary>
    ///// Optional: if the player is detected, update the pathfinding destination.
    ///// </summary>
    //private void UpdateBehavior()
    //{
    //    if (!chasePlayerWhenSeen) return;

    //    Transform player = perception.GetSeenPlayer();
    //    if (player != null)
    //    {
    //        pathfinding.SetDestination(player.position);
    //    }
    //}
}
