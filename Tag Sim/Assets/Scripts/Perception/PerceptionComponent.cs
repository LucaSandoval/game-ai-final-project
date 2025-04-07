using UnityEngine;

public class PerceptionComponent : MonoBehaviour
{
    [Header("Perception Settings")]
    [SerializeField] private float VisionAngle = 50f;
    [SerializeField] private float VisionDistance = 5f;

    private Vector2 lookDirection;
    private Rigidbody2D rb;
    public bool playerInSight;
    private GameObject player;


    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        // Register self with occupancy map component
        OccupancyMapController.Instance?.RegisterPerceiver(this, GetComponent<PathfindingComponent>());
        // Initialize look direction to up
        lookDirection = Vector2.up;
    }

    private void Update()
    {
        // Update look direction based on movement velocity
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            if (!playerInSight)
            {
                lookDirection = rb.linearVelocity.normalized;
            }
            else
            {
                lookDirection = (player.transform.position - transform.position).normalized;
            }
        }
    }

    /// <summary>
    /// Checks if the given tile is within the perceiver's line of sight.
    /// </summary>
    public bool HasLOS(GridTile tile)
    {
        GridComponent grid = GridComponent.Instance;
        if (!grid) return false;

        // if this is the tile im standing on, we also count that
        if (grid.GetGridTileAtWorldPosition(transform.position).GridCoordinate == tile.GridCoordinate) return true;

        // Line trace function so we don't have to raycast
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
                if (!currentCell.Traversable || !currentCell.EnemyTraversable) return false;

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

        Vector2 DirectionToTarget = tile.WorldPosition - (Vector2)transform.position;
        // Calculate the squared distance to our target. If it's greater than our vision distance
        // then target is outside cone of vision.
        float SquaredDistance = DirectionToTarget.sqrMagnitude;
        if (SquaredDistance < VisionDistance * VisionDistance)
        {
            DirectionToTarget.Normalize();
            float DotProduct = Vector2.Dot(lookDirection, DirectionToTarget);
            float CosVisionAngle = Mathf.Cos(VisionAngle * 0.5f * Mathf.Deg2Rad);
            // We use the dot product to derive the angle between our forward vector and vector to target
            // this will determine if target is within cone of vision.
            if (DotProduct >= CosVisionAngle)
            {
                return LineTrace(grid.GetGridTileAtWorldPosition(transform.position), tile);
            }
        }

        return false;
    }
}

