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
    // Drag your World Space Speech Bubble Prefab here.
    public TextMeshPro textBooble;
    // How long will the bubble display?
    public float speechBubbleDuration = 2f;
    // Position offset above the enemy for the bubble.
    public Vector3 bubbleOffset = new Vector3(0, 2f, 0);

    [Header("Bark Timing")]
    // Prevent rapid repeat barks.
    public float barkCooldown = 5f;
    private float lastBarkTime = -Mathf.Infinity;

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
    /// Instantiates and displays a speech bubble with the provided message.
    /// </summary>
    private void ShowSpeechBubble(string message)
    {

        // Instantiate the speech bubble as a child of this AI
        //GameObject bubble = Instantiate(speechBubblePrefab, transform.position + bubbleOffset, Quaternion.identity, transform);
        // Get the Text component from the bubble

        textBooble.gameObject.SetActive(true);
        textBooble.text = message;
        // Automatically disable the bubble after the desired duration
        Invoke(nameof(fish), speechBubbleDuration);
    }

    void fish()
    {

        textBooble.gameObject.SetActive(false);
    }
}