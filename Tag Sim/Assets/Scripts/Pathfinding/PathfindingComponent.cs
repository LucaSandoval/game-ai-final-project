using UnityEngine;
using System.Collections.Generic;
using System;

public class PathfindingComponent : MonoBehaviour
{
    private MovementComponent movementComponent;
    private Vector2 destination;

    private void Awake()
    {
        movementComponent = GetComponent<MovementComponent>();
    }

    private void Start()
    {
        destination = GridComponent.Instance.GetTile(5, 3).WorldPosition;
    }

    private void Update()
    {
        movementComponent.SetMovementPath(AStar(transform.position, destination));
    }

    public List<Vector2> AStar(Vector2 startPosition, Vector2 targetPosition)
    {
        GridComponent grid = GridComponent.Instance;
        if (!grid)
        {
            Debug.Log("No Grid Component found!");
            return new List<Vector2>();
        }

        // Start/end tile of search.
        GridTile startTile = grid.GetGridTileAtWorldPosition(startPosition);
        GridTile finalTile = grid.GetGridTileAtWorldPosition(targetPosition);
        // Came from map starts as empty
        Dictionary<GridTile, GridTile> cameFrom = new Dictionary<GridTile, GridTile>();
        // All gScores are initialized to infinity, start cell is 0.
        Dictionary<GridTile, float> gScore = new Dictionary<GridTile, float>();
        gScore.Add(startTile, 0);
        // All fScores are initialized to infinity, start cell is its heuristic
        // distance from destination.
        Dictionary<GridTile, float> fScore = new Dictionary<GridTile, float>();
        fScore.Add(startTile, grid.DistanceBetweenTiles(startTile, finalTile));

        // Comparator for our heap to sort by FScore
        Comparison<GridTile> compareFScore = (GridTile a, GridTile b) =>
        {
            float fA = fScore.ContainsKey(a) ? fScore[a] : float.MaxValue;
            float fB = fScore.ContainsKey(b) ? fScore[b] : float.MaxValue;
            return fA.CompareTo(fB);
        };

        // Open set (our heap) contains starting cell by default
        PriorityQueue<GridTile> openSet = new PriorityQueue<GridTile>(compareFScore);
        openSet.Enqueue(startTile);

        while (openSet.Count > 0)
        {
            // Get lowest fScore cell (most promising) off the heap.
            GridTile currentTile = openSet.Dequeue();
            // If we've found the destination, we're done.
            if (currentTile == finalTile)
            {
                List<Vector2> stepsOut = new List<Vector2>();

                // Add in destination tile
                stepsOut.Add(currentTile.WorldPosition);

                while(cameFrom.ContainsKey(currentTile))
                {
                    currentTile = cameFrom[currentTile];
                    // Add step into the path
                    stepsOut.Insert(0, currentTile.WorldPosition);
                }
                stepsOut.RemoveAt(0);
                return stepsOut;
            }

            // Tile neighbors
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

            foreach(GridTile neigbor in neighbors)
            {
                // Check if neighbor is within grid bounds
                if (neigbor == null) continue;
                // Check if this neighbor is inaccessible
                if (!neigbor.Traversable) continue;

                float tentativeGScore = gScore[currentTile] + grid.DistanceBetweenTiles(currentTile, neigbor);
                // Check if this path to the neighbor is better than any previous one
                if (!gScore.ContainsKey(neigbor) || tentativeGScore < gScore[neigbor])
                {
                    // If so, record it
                    cameFrom[neigbor] = currentTile;
                    gScore[neigbor] = tentativeGScore;
                    fScore[neigbor] = tentativeGScore + grid.DistanceBetweenTiles(finalTile, neigbor);

                    if (!openSet.Contains(neigbor))
                    {
                        openSet.Enqueue(neigbor);
                    }
                }
            }
        }


        return new List<Vector2>();
    }
}
