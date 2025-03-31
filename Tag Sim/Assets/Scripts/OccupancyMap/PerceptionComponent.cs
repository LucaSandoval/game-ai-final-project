using UnityEngine;
using System.Collections.Generic;

public class PerceptionComponent : MonoBehaviour
{
    [Header("Perception Settings")]
    [SerializeField] private float maxVisionRadius = 5f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Vision Curve Settings")]
    [SerializeField] private AnimationCurve visionFalloff;

    private GridComponent grid;
    private Transform player;
    private List<GridTile> visibleTiles = new List<GridTile>();

    private void Awake()
    {
        grid = GridComponent.Instance;
    }

    private void Update()
    {
        UpdatePerception();
    }

    /// <summary>
    /// Updates perception and vision of the AI.
    /// </summary>
    private void UpdatePerception()
    {
        visibleTiles.Clear();

        // Get the player's position
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxVisionRadius, playerMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                break;
            }
        }

        // Get Dijkstra map to determine visible tiles
        (GridMap visibilityMap, _) = grid.test.Dijkstra(transform.position);
        for (int h = 0; h < grid.GetGridDimensions().Item2; h++)
        {
            for (int w = 0; w < grid.GetGridDimensions().Item1; w++)
            {
                GridTile tile = grid.GetGridData().GetTile(w, h);
                float distance = visibilityMap.GetGridValue(w, h);
                float visionStrength = visionFalloff.Evaluate(distance / maxVisionRadius);

                if (distance <= maxVisionRadius && IsTileWithinFOV(tile))
                {
                    tile.Visible = true;
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
        float angleToTile = Vector2.Angle(transform.right, directionToTile);

        return angleToTile <= visionAngle / 2;
    }

    /// <summary>
    /// Gets all visible tiles.
    /// </summary>
    public List<GridTile> GetVisibleTiles()
    {
        return visibleTiles;
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
    }
}

