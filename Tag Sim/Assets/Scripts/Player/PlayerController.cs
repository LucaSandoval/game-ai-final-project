using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private GridComponent gridComponent;
    private MovementComponent movementComponent;

    public Vector2 playerPos;

    private float originalSpeed;
    private float sprintSpeed;

    // Checks for Stamina during sprinting
    // StaminaDrain/Regen is measured in rate per second
    private float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    private float staminaDrain = 25f;
    private float staminaRegen = 10f;
    private bool isSprinting;

    public Image staminaBar;
    public CanvasGroup staminaCanvasGroup;

    private Vector2 lastPosition; // Track the last position for movement check

    private int score = 0;
    private Tilemap goalTilemap;
    public Tile BaseTile;
    public Tile GoalTile;

    public static int GoalTilesLeft;
    public Sound GetGoalTileSound;

    private void Awake()
    {
        GoalTilesLeft = 5;
    }

    private void Start()
    {
        gridComponent = GridComponent.Instance;
        movementComponent = GetComponent<MovementComponent>();

        goalTilemap = gridComponent.GetTilemap();

        originalSpeed = movementComponent.GetSpeed();
        sprintSpeed = originalSpeed * 2;

        currentStamina = maxStamina;
        lastPosition = transform.position;

        if (staminaCanvasGroup == null)
        {
            Debug.LogWarning("CanvasGroup for stamina UI is null");
        }
    }

    private void Update()
    {
        HandleStamina();
        CheckGoalTile(); // Check if the player is on a "Goal" tile
    }

    // Handle the stamina of the player when sprinting
    private void HandleStamina()
    {
        // if the player is moving, then drain stamina while sprinting
        bool isMoving = (Vector2)transform.position != lastPosition;

        if (isSprinting && isMoving)
        {
            currentStamina -= staminaDrain * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isSprinting = false;
                movementComponent.SetSpeed(originalSpeed);
            }
        }
        else
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        lastPosition = transform.position;
        staminaBar.fillAmount = currentStamina / maxStamina;

        // Stamina bar fades out when stamina is full
        if (staminaCanvasGroup != null)
        {
            float targetAlpha = (currentStamina < maxStamina) ? 1f : 0f;
            staminaCanvasGroup.alpha = Mathf.Lerp(staminaCanvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }
    }

    // Check if the player is on a "Goal" tile and increment the score
    private void CheckGoalTile()
    {
        if (goalTilemap == null) return;

        // Get the player's current position in the tilemap's grid
        Vector3Int tilePosition = goalTilemap.WorldToCell(transform.position);

        // Get the tile at the player's position
        TileBase tile = goalTilemap.GetTile(tilePosition);

        if (tile != null && tile.name == "Goal")
        {
            SoundController.Instance?.PlaySound(GetGoalTileSound);
            if (GoalTilesLeft > 0) GoalTilesLeft -= 1;
            score++;
            Debug.Log($"Score: {score}");
            goalTilemap.SetTile(tilePosition, BaseTile);
            SetNewGoalTile(tilePosition);
        }
    }

    // Get the current score for use in the GameOver screen.
    public int GetScore()
    {
        return score;
    }

    // Once the player has stepped on a goal tile, generate a new random one 10 tiles away
    private void SetNewGoalTile(Vector3Int currentTilePosition)
    {
        if (goalTilemap == null) return;

        Vector3Int gridSize = goalTilemap.size;
        Vector3Int newGoalPosition;

        do
        {
            // Generate a random position within the bounds of the tilemap
            int randomX = Random.Range(goalTilemap.origin.x, goalTilemap.origin.x + gridSize.x);
            int randomY = Random.Range(goalTilemap.origin.y, goalTilemap.origin.y + gridSize.y);
            newGoalPosition = new Vector3Int(randomX, randomY, currentTilePosition.z);

        } while (Vector3Int.Distance(newGoalPosition, currentTilePosition) < 10 ||
                 goalTilemap.GetTile(newGoalPosition) != BaseTile);

        // Set the new GoalTile
        goalTilemap.SetTile(newGoalPosition, GoalTile);
    }

    // Get the destination of the player based on the key pressed
    public Vector2 getDestination()
    {
        gridComponent = GridComponent.Instance;
        GridTile currentPos = gridComponent.GetGridTileAtWorldPosition(transform.position);
        Vector2Int targetCoord = currentPos.GridCoordinate;

        // Check for movement inputs + sprint input
        bool moveUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool moveDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool moveLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool moveRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isSprinting && currentStamina > 0)
        {
            movementComponent.SetSpeed(sprintSpeed);
        }
        else
        {
            movementComponent.SetSpeed(originalSpeed);
        }

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
        if (targetTile != null && targetTile.Traversable)
        {
            return targetTile.WorldPosition;
        }
        else
        {
            return transform.position;
        }
    }
}