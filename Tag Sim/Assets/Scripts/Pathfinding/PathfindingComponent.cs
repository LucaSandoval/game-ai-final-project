using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Component responsible for all things related to pathfinding an entity, providing AStar, Smoothing,
/// and other related methods.
/// </summary>
public class PathfindingComponent : MonoBehaviour
{
    private MovementComponent movementComponent;
    private Vector2 destination;

    [SerializeField] private bool isPlayer = false;
    private PlayerController playerController;

    private void Awake()
    {
        movementComponent = GetComponent<MovementComponent>();

        if (isPlayer)
        {
            playerController = GetComponent<PlayerController>();
        }
    }

    private void Start()
    {
        if (isPlayer == false)
        {
            destination = GridComponent.Instance.GetTile(0, 0).WorldPosition;
        }
        else
        {
            destination = GridComponent.Instance.GetTile(1, 0).WorldPosition;
        }

    }

    private void Update()
    {
        if (movementComponent)
        {
            List<GridTile> astarPath = AStar(transform.position, destination);
            List<GridTile> smoothedPath = SmoothPath(astarPath);
            movementComponent.SetMovementPath(ConvertTilePathToMovementPath(smoothedPath));

            if(Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                destination = mousePos;
             }

            // Player Movement
            if (isPlayer)
            {
                Vector2 targetPos = playerController.getDestination();

                if (targetPos != destination)
                {
                    destination = targetPos;
                }
            }
        }
    }

    /// <summary>
    /// Converts a list of grid tiles into a list of world positions for use in the movement component.
    /// </summary>
    public List<Vector2> ConvertTilePathToMovementPath(List<GridTile> tilePath)
    {
        List<Vector2> movementPath = new List<Vector2>();
        foreach(GridTile tile in tilePath)
        {
            movementPath.Add(tile.WorldPosition);
        }
        return movementPath;
    }

    /// <summary>
    /// Given a path of grid tiles, smooths them out by removing uncessesary stepping and leaves
    /// only the most necessary tiles.
    /// </summary>
    public List<GridTile> SmoothPath(List<GridTile> rawPath)
    {
        GridComponent grid = GridComponent.Instance;
        if (!grid)
        {
            Debug.Log("No Grid Component found!");
            return new List<GridTile>();
        }

        // Define our line trace function to determine if a straight line can be
        // drawn between two cells without colliding with a non-traversable cell along
        // the path.
        System.Func<GridTile, GridTile, bool> LineTrace = (GridTile start, GridTile end) =>
        {
            int x1 = start.GridCoordinate.x, y1 = start.GridCoordinate.y;
            int x2 = end.GridCoordinate.x, y2 = end.GridCoordinate.y;

            int dx = Mathf.Abs(x2 - x1);
            int dy = Mathf.Abs(y2 - y1);
            int sx = (x1 < x2) ? 1 : -1;
            int sy = (y1 < y2) ? 1 : -1;

            int err = dx - dy;

            while (true)
            {
                GridTile currentCell = grid.GetTile(x1, y1);

                // Bounds check
                if (currentCell == null) return false;
                    
                // Traversability check
                if (!currentCell.Traversable) return false;

                // Success condition
                if (currentCell == end) return true;

                // Move to the next cell
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        };

        // Our final path will keep only the steps needed to get to the target
        List<GridTile> finalPath = new List<GridTile>();
        if (rawPath.Count <= 2 || !LineTrace(grid.GetGridTileAtWorldPosition(transform.position), rawPath[1]))
        {
            finalPath = rawPath;
        } else
        {
            // We iterate through each step of the path, tracing as far down the path as we can
            // get.
            for (int i = 0; i < rawPath.Count;)
            {
                for (int j = i + 1; j < rawPath.Count; j++)
                {
                    // If we find a tile along the path we can't get to, we add the last step
                    // to our final path and then start again from there.
                    if (!LineTrace(rawPath[i], rawPath[j]))
                    {
                        i = j - 1;
                        finalPath.Add(rawPath[i]);
                        break;
                    }
                    else if (j == rawPath.Count - 1)
                    {
                        // If we've made it from our i-tile to the last tile (destination) without being
                        // blocked, we add that point in to our final path and exit the search; our
                        // path is now complete.
                        i = rawPath.Count;
                        finalPath.Add(rawPath[j]);
                    }
                }
            }
        }
        return finalPath;
    }

    /// <summary>
    /// Performs an AStar search from the start to the end position. Returns a list of tiles
    /// representing the path from the start to the end.
    /// </summary>
    public List<GridTile> AStar(Vector2 startPosition, Vector2 targetPosition)
    {
        GridComponent grid = GridComponent.Instance;
        if (!grid)
        {
            Debug.Log("No Grid Component found!");
            return new List<GridTile>();
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
                List<GridTile> stepsOut = new List<GridTile>();

                // Add in destination tile
                stepsOut.Add(currentTile);

                while(cameFrom.ContainsKey(currentTile))
                {
                    currentTile = cameFrom[currentTile];
                    // Add step into the path
                    stepsOut.Insert(0, currentTile);
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

        //Debug.Log("No path found!");
        return new List<GridTile>();
    }

    /// <summary>
    /// Performs Dijkstra's algorithm across the grid from the given start position. Returns a new GridMap containing
    /// the Dijkstra values and a map containing the shortests paths (prev).
    /// </summary>
    public (GridMap, Dictionary<GridTile, GridTile>) Dijkstra(Vector2 startPosition)
    {
        GridComponent grid = GridComponent.Instance;
        if (!grid)
        {
            Debug.Log("No Grid Component found!");
            return (null, null);
        }

        // Initialize variables
        GridTile startingTile = grid.GetGridTileAtWorldPosition(startPosition);
        GridMap distanceMapOut = new GridMap(grid.GetGridDimensions().Item1, grid.GetGridDimensions().Item2, float.MaxValue);
        Dictionary<GridTile, GridTile> prev = new Dictionary<GridTile, GridTile>();

        // Debug log for starting tile

        // Define our minimum distance comparator for our heap
        Comparison<GridTile> CompareMinimumDistance = (GridTile a, GridTile b) =>
        {
            float fA = distanceMapOut.GetGridValue(a.GridCoordinate.x, a.GridCoordinate.y);
            float fB = distanceMapOut.GetGridValue(b.GridCoordinate.x, b.GridCoordinate.y);
            return fA.CompareTo(fB);
        };

        // Initialize the distance of our source cell as 0
        distanceMapOut.SetGridValue(startingTile.GridCoordinate.x, startingTile.GridCoordinate.y, 0);

        // Push the first cell onto the heap
        PriorityQueue<GridTile> q = new PriorityQueue<GridTile>(CompareMinimumDistance);
        q.Enqueue(startingTile);

        // Main algorithm loop
        while (q.Count > 0)
        {
            // Get lowest fScore cell (most promising) off the heap.
            GridTile currentTile = q.Dequeue();

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

            foreach (GridTile neighbor in neighbors)
            {
                // Check if neighbor is within grid bounds
                if (neighbor == null) continue;
                // Check if this neighbor is inaccessible
                if (!neighbor.Traversable) continue;

                // Check if the dist[CurrentCell] + distance from CurrentCell to Neighbor
                // is less than dist[Neighbor].
                float Alt = distanceMapOut.GetGridValue(currentTile.GridCoordinate.x, currentTile.GridCoordinate.y);
                Alt += grid.DistanceBetweenTiles(currentTile, neighbor);

                float OldNeighborDist = distanceMapOut.GetGridValue(neighbor.GridCoordinate.x, neighbor.GridCoordinate.y);
                if (Alt < OldNeighborDist)
                {
                    distanceMapOut.SetGridValue(neighbor.GridCoordinate.x, neighbor.GridCoordinate.y, Alt);
                    prev[neighbor] = currentTile;

                    q.Enqueue(neighbor);
                }
            }
        }
        return (distanceMapOut, prev);
    }
}
