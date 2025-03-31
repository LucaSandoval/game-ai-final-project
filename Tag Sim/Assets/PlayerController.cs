using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GridComponent gridComponent;

    public Vector2 playerPos;

    private void Awake()
    {
        gridComponent = GridComponent.Instance;
    }


    // Get the destination of the player based on the key pressed
    // NOTE: Up and Down are reversed because of the way the grid is set up
    public Vector2 getDestination()
    {
        gridComponent = GridComponent.Instance;
        GridTile currentPos = gridComponent.GetGridTileAtWorldPosition(transform.position);
        Vector2Int targetCoord = currentPos.GridCoordinate;

        // Check for movement inputs
        bool moveUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool moveDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool moveLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool moveRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        // Handle movement combinations
        if (moveUp && moveRight) targetCoord += new Vector2Int(1, 1); // Up-Right
        else if (moveUp && moveLeft) targetCoord += new Vector2Int(-1, 1); // Up-Left
        else if (moveDown && moveRight) targetCoord += new Vector2Int(1, -1); // Down-Right
        else if (moveDown && moveLeft) targetCoord += new Vector2Int(-1, -1); // Down-Left
        else if (moveUp) targetCoord += new Vector2Int(0, 1); // Up
        else if (moveDown) targetCoord += new Vector2Int(0, -1);  // Down
        else if (moveLeft) targetCoord += new Vector2Int(-1, 0); // Left
        else if (moveRight) targetCoord += new Vector2Int(1, 0);  // Right

        // Get the target tile and return its world position
        GridTile targetTile = gridComponent.GetTile(targetCoord.x, targetCoord.y);
        return targetTile.WorldPosition;
    }
}
