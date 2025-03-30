using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{

    private GridComponent gridComponent;

    public Vector2 playerPos;

    private void Awake()
    {
        gridComponent = GridComponent.Instance;
    }

    private void Update()
    {

    }


    // This actually makes the player move
    // NOTE: Up and Down are reversed because of the way the grid is set up
    public Vector2 getDestination(KeyCode keyCode)
    {
        gridComponent = GridComponent.Instance;
        GridTile currentPos = gridComponent.GetGridTileAtWorldPosition(transform.position);
        Vector2Int targetCoord = currentPos.GridCoordinate;

        if (keyCode == KeyCode.W || keyCode == KeyCode.UpArrow)
            targetCoord += Vector2Int.down;
        else if (keyCode == KeyCode.A || keyCode == KeyCode.LeftArrow)
            targetCoord += Vector2Int.left;
        else if (keyCode == KeyCode.S || keyCode == KeyCode.DownArrow) 
            targetCoord += Vector2Int.up;
        else if (keyCode == KeyCode.D || keyCode == KeyCode.RightArrow)
            targetCoord += Vector2Int.right;
        else
            return transform.position;

        GridTile targetTile = gridComponent.GetTile(targetCoord.x, targetCoord.y);
        return targetTile.WorldPosition;
    }
}