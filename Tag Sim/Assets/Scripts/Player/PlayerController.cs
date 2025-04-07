using UnityEngine;
using System.Collections.Generic;
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

    private void Awake()
    {
        gridComponent = GridComponent.Instance;
        movementComponent = GetComponent<MovementComponent>();

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