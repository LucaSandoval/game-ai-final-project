using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    //[SerializeField] private float movementDuration = 0.2f;

    private GridComponent gridComponent;
    MovementComponent movement;
    PathfindingComponent pathfinding;
    //private bool isMoving = false;
    //private Vector2 targetPosition;
    //private Vector2 startPosition;
    //private float moveTimer = 0f;

    public Vector2 playerPos;

    private void Awake()
    {
        gridComponent = GridComponent.Instance;
        
        // I don't actually need these rn
        movement = GetComponent<MovementComponent>();
        pathfinding = GetComponent<PathfindingComponent>();
    }

    private void Update()
    {
        //// Only process input if we're not already moving
        //if (!isMoving)
        //{
        //    // Check for movement input
        //    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        //    {
        //        Debug.Log("I am trying to move Up");
        //        TryMove(Vector2Int.up);
        //    }
        //    else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        //    {
        //        Debug.Log("I am trying to move Down");
        //        TryMove(Vector2Int.down);
        //    }
        //    else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        //    {
        //        Debug.Log("I am trying to move Left");
        //        TryMove(Vector2Int.left);
        //    }
        //    else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        //    {
        //        Debug.Log("I am trying to move Right");
        //        TryMove(Vector2Int.right);
        //    }
        //}
        //else
        //{
        //    // Handle movement animation
        //    moveTimer += Time.deltaTime;
        //    float t = Mathf.Clamp01(moveTimer / movementDuration);

        //    // Lerp position
        //    transform.position = Vector2.Lerp(startPosition, targetPosition, t);

        //    // Check if movement is complete
        //    if (t >= 1.0f)
        //    {
        //        transform.position = targetPosition;
        //        isMoving = false;
        //        moveTimer = 0f;
        //    }
        //}
    }

    // Literal biohazard code bro please forgive me this shit doesnt even do anything.
    private void TryMove(Vector2Int direction)
    {
        gridComponent = GridComponent.Instance;

        Debug.Log("I am in TryMove");

        // Get current position and tile
        GridTile currentPos = gridComponent.GetGridTileAtWorldPosition(transform.position);
        playerPos = transform.position;
        //GridTile currentTile = gridComponent.GetGridTileAtWorldPosition(currentPos);
        

        if (currentPos == null)
        {
            Debug.LogWarning("Player is outside the grid");
            return;
        }

        Debug.Log("My CurrentTile is " + currentPos.GridCoordinate.ToString());

        // Calculate target grid coordinate
        Vector2Int targetCoord = currentPos.GridCoordinate + direction;
        Debug.Log("TargetCoord is " + targetCoord.ToString());


        GridTile targetTile = gridComponent.GetTile(targetCoord.x, targetCoord.y);

        if (targetTile == null)
        {
            Debug.LogWarning("Target tile is null");
            return;
        }

        // Check if target tile exists and is traversable
        if (targetTile != null)
        {
            // Create a path with the world position of the target tile
            List<Vector2> path = new List<Vector2>();
            path.Add(targetTile.WorldPosition);

            if (path.Count == 0)
            {
                Debug.LogWarning("Path is empty");
                return;
            } else
            {
                Debug.Log("Path is not empty");
            }



                // Set the movement path
            

            List<GridTile> astarPath = pathfinding.AStar(transform.position, path[0]);
            List<GridTile> smoothedPath = pathfinding.SmoothPath(astarPath);
            movement.SetMovementPath(pathfinding.ConvertTilePathToMovementPath(smoothedPath));
            //movement.StopMovement(); // First stop any current movement
            //movement.SetMovementPath(path);

            //Debug.Log("Setting path to world position: " + targetTile.WorldPosition);
        }
        else
        {
            Debug.Log("Target tile is null or not traversable");
        }
    }

    // This actually makes the player move
    // NOTE: Up and Down are reversed because of the way the grid is set up
    public Vector2 getDestination(KeyCode keyCode)
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            GridComponent grid = GridComponent.Instance;
            GridTile currentPos = grid.GetGridTileAtWorldPosition(transform.position);
            Vector2Int targetCoord = currentPos.GridCoordinate + Vector2Int.down;
            GridTile targetTile = grid.GetTile(targetCoord.x, targetCoord.y);
            return targetTile.WorldPosition;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            GridComponent grid = GridComponent.Instance;
            GridTile currentPos = grid.GetGridTileAtWorldPosition(transform.position);
            Vector2Int targetCoord = currentPos.GridCoordinate + Vector2Int.left;
            GridTile targetTile = grid.GetTile(targetCoord.x, targetCoord.y);
            return targetTile.WorldPosition;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            GridComponent grid = GridComponent.Instance;
            GridTile currentPos = grid.GetGridTileAtWorldPosition(transform.position);
            Vector2Int targetCoord = currentPos.GridCoordinate + Vector2Int.up;
            GridTile targetTile = grid.GetTile(targetCoord.x, targetCoord.y);
            return targetTile.WorldPosition;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            GridComponent grid = GridComponent.Instance;
            GridTile currentPos = grid.GetGridTileAtWorldPosition(transform.position);
            Vector2Int targetCoord = currentPos.GridCoordinate + Vector2Int.right;
            GridTile targetTile = grid.GetTile(targetCoord.x, targetCoord.y);
            return targetTile.WorldPosition;
        }
        else
        {
            return transform.position;
        }
    }
}