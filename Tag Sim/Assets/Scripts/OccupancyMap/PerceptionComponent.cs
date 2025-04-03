using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PerceptionComponent : MonoBehaviour
{
    [Header("Perception Settings")]
    [SerializeField] private float maxVisionRadius = 5f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Vision Curve Settings")]
    [SerializeField] private AnimationCurve visionFalloff;

    private GridComponent grid;
    private Transform player;
    private MovementComponent movementComponent;
    private List<GridTile> visibleTiles = new List<GridTile>();

    private void Awake()
    {
        grid = GridComponent.Instance;
        movementComponent = GetComponent<MovementComponent>();
    }

    private void Update()
    {
        UpdatePerception();
    }

    /// <summary>
    /// Updates perception and vision of the AI.
    /// </summary>
    public void UpdatePerception()
    {
        visibleTiles.Clear();
        player = null; //So that the Ai doesn't keep the last seen player in memory

        if (grid == null) grid = GridComponent.Instance;
        if (grid == null)
        {
            Debug.LogError("PerceptionComponent: GridComponent is null.");
            return;
        }

        PathfindingComponent pathfinding = GetComponent<PathfindingComponent>();
        if (pathfinding == null)
        {
            Debug.LogError("PerceptionComponent: PathfindingComponent missing.");
            return;
        }

        // Get player
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxVisionRadius);
        foreach (Collider2D hit in hits)
        {
            Debug.Log($"Checking collider: {hit.name}");
            if (hit.CompareTag("Player"))
            {
                Debug.Log("Player detected (fallback no-layer check)");
                player = hit.transform;
                break;
            }
        }

        // Get visibility map
        (GridMap visibilityMap, _) = pathfinding.Dijkstra(transform.position);
        if (visibilityMap == null) return;

        var (width, height) = grid.GetGridData().GetGridSize();

        for (int h = 0; h < height; h++)
        {
            for (int w = 0; w < width; w++)
            {
                GridTile tile = grid.GetGridData().GetTile(w, h);
                if (tile == null) continue;

                float distance = visibilityMap.GetGridValue(w, h);
                float visionStrength = visionFalloff != null ? visionFalloff.Evaluate(distance / maxVisionRadius) : 1f;

                if (
                    distance <= maxVisionRadius &&
                    IsTileWithinFOV(tile) &&
                    HasLineOfSight(tile)
                )
                {
                    tile.Visible = true;
                    tile.LastSeenTime = Time.time;
                    visibleTiles.Add(tile);
                }
                else
                {
                    tile.Visible = false;
                }
            }
        }
    }



    /// <summary>
    /// Checks if a tile is within the AI's field of view.
    /// </summary>
    private bool IsTileWithinFOV(GridTile tile)
    {
        Vector2 directionToTile = (tile.WorldPosition - (Vector2)transform.position).normalized;

        Vector2 facing = movementComponent != null ? movementComponent.GetCurrentVelocity().normalized : Vector2.right;

        if (facing == Vector2.zero) facing = Vector2.right; // Default fallback direction

        float angleToTile = Vector2.Angle(facing, directionToTile);
        return angleToTile <= visionAngle / 2f;
    }


    /// <summary>
    /// Gets all visible tiles.
    /// </summary>
    public List<GridTile> GetVisibleTiles()
    {
        return visibleTiles;
    }
    
    /// <summary>
    /// returns the Players location if it is in the line of sight of the enemy AI 
    /// </summary>
    public Transform GetSeenPlayer()
    {
        return player;
    }

    private bool HasLineOfSight(GridTile tile)
    {
        Vector2 start = transform.position;
        Vector2 end = tile.WorldPosition;
        Vector2 dir = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        RaycastHit2D[] hits = Physics2D.RaycastAll(start, dir, distance, obstacleMask);

        foreach (RaycastHit2D hit in hits)
        {
            // If it hit something that ISN'T the player, return false
            if (!hit.collider.CompareTag("Player"))
            {
                Debug.Log($"Blocked by: {hit.collider.name}");
                return false;
            }
        }

        return true;
    }


    /// <summary>
    /// Draws the AI's vision radius and field of view in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxVisionRadius);

        Gizmos.color = Color.blue;
        Vector3 leftSide = Quaternion.Euler(0, 0, visionAngle / 2) * transform.right * maxVisionRadius;
        Vector3 rightSide = Quaternion.Euler(0, 0, -visionAngle / 2) * transform.right * maxVisionRadius;

        Gizmos.DrawLine(transform.position, transform.position + leftSide);
        Gizmos.DrawLine(transform.position, transform.position + rightSide);

        Gizmos.color = Color.cyan;

        if (movementComponent != null)
        {
            Vector2 facing = movementComponent.GetCurrentVelocity().normalized;
            if (facing != Vector2.zero)
            {
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)facing * maxVisionRadius);
            }
        }
    }
}

