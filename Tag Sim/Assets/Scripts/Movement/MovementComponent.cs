using UnityEngine;
using System.Collections.Generic;

public class MovementComponent : MonoBehaviour
{
    [SerializeField] private float MovementSpeed;
    [SerializeField] private float ArrivalDistance;
    [SerializeField] private bool DebugVisualization;

    private Rigidbody2D rb;
    private List<Vector2> movementPath;
    private bool moving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementPath = new List<Vector2>();
    }

    public void SetMovementPath(List<Vector2> path)
    {
        if (moving) return;
        movementPath = path;
    }

    public void StopMovement()
    {
        movementPath.Clear();
        rb.linearVelocity = Vector2.zero;
        moving = false;
    }

    private void FixedUpdate()
    {
        // Only move if we have a path at all
        if (movementPath.Count > 0)
        {
            // If we are within arrival distance of the first point on our path, stop.
            if (Vector2.Distance(transform.position, movementPath[0]) <= ArrivalDistance)
            {
                StopMovement();
            } else
            {
                // Calculate direction vector
                Vector2 dir = (movementPath[0] - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * MovementSpeed;
                moving = true;
            }

        } else
        {
            StopMovement();
        }
    }

    public Vector2 GetCurrentVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }


    private void Update()
    {
        if (DebugVisualization && movementPath.Count > 0)
        {
            Debug.DrawLine(transform.position, movementPath[0], Color.green);
            for (int i = 0; i < movementPath.Count - 1; i++)
            {
                Debug.DrawLine(movementPath[i], movementPath[i + 1], Color.green);
            }
        }
    }
}
