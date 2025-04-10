using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AiBarkController : MonoBehaviour
{
    public enum AIType
    {
        Chaser,
        Hider,
        Predictor,
        Blocker
    }

    [Header("AI Settings")]
    // Distinguish this enemy type (set from the Inspector)
    public AIType aiType;
    // Reference the corresponding bark data asset for this AI.
    public AiBarkData barkData;

    [Header("Speech Bubble Settings")]
    // Direct reference to the TextMeshPro component for speech bubble
    public TextMeshPro textBooble;
    // How long will the bubble display?
    public float speechBubbleDuration = 2f;
    // Position offset above the enemy for the bubble.
    public Vector3 bubbleOffset = new Vector3(0, 2f, 0);

    [Header("Bark Timing")]
    // Prevent rapid repeat barks.
    public float barkCooldown = 5f;
    private float lastBarkTime = -Mathf.Infinity;

    [Header("Acknowledgment Lines")]
    [Tooltip("Lines this AI will say when responding to another AI's alert")]
    [TextArea(2, 5)]
    public string[] acknowledgmentLines = new string[] {
        "Affirmative!",
        "On it!",
        "Copy that!",
        "Understood!",
        "Moving there now!"
    };

    [Header("Player Lost Lines")]
    [Tooltip("Lines this AI will say when reporting the player is lost")]
    [TextArea(2, 5)]
    public string[] playerLostLines = new string[] {
        "I've lost them!",
        "Target disappeared!",
        "They're gone!",
        "Where did they go?"
    };

    private PerceptionComponent perception;
    private bool wasPlayerInSight = false;

    private void Start()
    {
        // Register with the communication system
        if (AiComunication.Instance != null)
        {
            AiComunication.Instance.RegisterAiController(this);
        }

        // Get the perception component
        perception = GetComponent<PerceptionComponent>();

        // Make sure the text bubble is initially hidden
        if (textBooble != null)
        {
            textBooble.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Track if player was in sight last frame
        bool playerIsInSight = perception != null && perception.playerInSight;

        // If we see the player and we haven't barked recently, report to the communication system
        if (playerIsInSight && Time.time - lastBarkTime >= barkCooldown)
        {
            if (AiComunication.Instance != null)
            {
                AiComunication.Instance.ReportPlayerSpotted(this);
            }
            else
            {
                // If there's no communication system, just bark directly
                BarkPlayerSpotted();
            }
        }

        // Check if all AIs have lost the player
        if (wasPlayerInSight && !playerIsInSight)
        {
            // Check if any AI still sees the player
            bool anyAiSeesPlayer = false;
            AiBarkController[] allControllers = FindObjectsByType<AiBarkController>(FindObjectsSortMode.None);

            foreach (AiBarkController controller in allControllers)
            {
                PerceptionComponent otherPerception = controller.GetComponent<PerceptionComponent>();
                if (otherPerception != null && otherPerception.playerInSight)
                {
                    anyAiSeesPlayer = true;
                    break;
                }
            }

            // If no AI sees the player, report lost
            if (!anyAiSeesPlayer && AiComunication.Instance != null)
            {
                AiComunication.Instance.ReportPlayerLost();
            }
        }

        // Update previous state
        wasPlayerInSight = playerIsInSight;
    }

    /// <summary>
    /// Call this method when the AI spots the player.
    /// </summary>
    public void BarkPlayerSpotted()
    {
        if (Time.time - lastBarkTime < barkCooldown)
            return;

        lastBarkTime = Time.time;
        if (barkData != null && barkData.playerSpottedLines.Length > 0)
        {
            string message = barkData.playerSpottedLines[Random.Range(0, barkData.playerSpottedLines.Length)];
            ShowSpeechBubble(message);
        }
    }

    /// <summary>
    /// Call this when the AI is acknowledging another AI's alert
    /// </summary>
    public void BarkAcknowledge()
    {
        // No cooldown check for acknowledgments since they're managed by the communication system
        string message = "";
        if (acknowledgmentLines.Length > 0)
        {
            message = acknowledgmentLines[Random.Range(0, acknowledgmentLines.Length)];
        }
        else if (barkData != null && barkData.occupancyBarkLines.Length > 0)
        {
            // Fall back to occupancy lines if no acknowledgment lines are defined
            message = barkData.occupancyBarkLines[Random.Range(0, barkData.occupancyBarkLines.Length)];
        }

        if (!string.IsNullOrEmpty(message))
        {
            ShowSpeechBubble(message);
        }

        lastBarkTime = Time.time;
    }

    /// <summary>
    /// Call this when the AI reports the player is lost
    /// </summary>
    public void BarkPlayerLost()
    {
        // No cooldown check since this is managed by the communication system

        string message = "";
        if (playerLostLines.Length > 0)
        {
            message = playerLostLines[Random.Range(0, playerLostLines.Length)];
        }
        else if (barkData != null && barkData.confusedLines != null && barkData.confusedLines.Length > 0)
        {
            message = barkData.confusedLines[Random.Range(0, barkData.confusedLines.Length)];
        }

        if (!string.IsNullOrEmpty(message))
        {
            ShowSpeechBubble(message);
        }

        lastBarkTime = Time.time;
    }

    /// <summary>
    /// Call this when the AI updates its occupancy map (or similar area-related checks).
    /// </summary>
    public void BarkOccupancyUpdate()
    {
        if (Time.time - lastBarkTime < barkCooldown)
            return;

        lastBarkTime = Time.time;
        if (barkData != null && barkData.occupancyBarkLines.Length > 0)
        {
            string message = barkData.occupancyBarkLines[Random.Range(0, barkData.occupancyBarkLines.Length)];
            ShowSpeechBubble(message);
        }
    }

    /// <summary>
    /// Call this when the AI performs a path prediction or similar event.
    /// </summary>
    public void BarkPathPrediction()
    {
        if (Time.time - lastBarkTime < barkCooldown)
            return;

        lastBarkTime = Time.time;
        if (barkData != null && barkData.pathPredictionLines.Length > 0)
        {
            string message = barkData.pathPredictionLines[Random.Range(0, barkData.pathPredictionLines.Length)];
            ShowSpeechBubble(message);
        }
    }

    /// <summary>
    /// Gets the duration of this AI's barks
    /// </summary>
    public float GetBarkDuration()
    {
        // Use barkData override if available
        if (barkData != null && barkData.barkDuration > 0)
        {
            return barkData.barkDuration;
        }

        return speechBubbleDuration;
    }

    /// <summary>
    /// Displays a speech bubble with the provided message.
    /// </summary>
    private void ShowSpeechBubble(string message)
    {
        if (textBooble == null)
        {
            Debug.LogWarning("TextMeshPro component not assigned in AiBarkController");
            return;
        }

        // Set color if specified in bark data
        if (barkData != null && barkData.bubbleColor != Color.white)
        {
            textBooble.color = barkData.bubbleColor;
        }

        // Display the speech bubble with the message
        textBooble.gameObject.SetActive(true);
        textBooble.text = message;

        // Get the appropriate duration
        float duration = GetBarkDuration();

        // Automatically disable the bubble after the desired duration
        CancelInvoke("HideSpeechBubble");
        Invoke("HideSpeechBubble", duration);
    }

    /// <summary>
    /// Hides the speech bubble
    /// </summary>
    private void HideSpeechBubble()
    {
        if (textBooble != null)
        {
            textBooble.gameObject.SetActive(false);
        }
    }
}
