using UnityEngine;

[CreateAssetMenu(fileName = "AiBarkData", menuName = "Scriptable Objects/AiBarkData")]
public class AiBarkData : ScriptableObject
{
    [Header("AI Information")]
    [Tooltip("A name for this AI type that might be shown in barks")]
    public string aiName = "Enemy";

    [Tooltip("The color to use for this AI's speech bubbles")]
    public Color bubbleColor = Color.white;

    [Header("When the AI sees the player")]
    [TextArea(2, 5)]
    public string[] playerSpottedLines;

    [Header("For occupancy map / area evaluation events")]
    [TextArea(2, 5)]
    public string[] occupancyBarkLines;

    [Header("For path prediction / target locking events")]
    [TextArea(2, 5)]
    public string[] pathPredictionLines;

    [Header("When the AI is idle")]
    [TextArea(2, 5)]
    public string[] idleLines;

    [Header("When the AI is searching but confused")]
    [TextArea(2, 5)]
    public string[] confusedLines;

    [Header("Timing Settings")]
    [Tooltip("How often this AI can bark when idle")]
    public float idleBarkFrequency = 15f;

    [Tooltip("Probability (0-1) of an idle bark occurring when the timer is up")]
    [Range(0, 1)]
    public float idleBarkProbability = 0.3f;

    [Tooltip("How long this AI's barks should display (overrides controller default if > 0)")]
    public float barkDuration = 0f;

    /// <summary>
    /// Gets a random line from the specified category, or null if the category is empty.
    /// </summary>
    public string GetRandomLine(BarkCategory category)
    {
        string[] lines = GetLinesForCategory(category);

        if (lines == null || lines.Length == 0)
            return null;

        return lines[Random.Range(0, lines.Length)];
    }

    /// <summary>
    /// Gets the array of lines for the specified category.
    /// </summary>
    private string[] GetLinesForCategory(BarkCategory category)
    {
        switch (category)
        {
            case BarkCategory.PlayerSpotted:
                return playerSpottedLines;
            case BarkCategory.Occupancy:
                return occupancyBarkLines;
            case BarkCategory.PathPrediction:
                return pathPredictionLines;
            case BarkCategory.Idle:
                return idleLines;
            case BarkCategory.Confused:
                return confusedLines;
            default:
                return null;
        }
    }

    /// <summary>
    /// Categories of barks that an AI can use.
    /// </summary>
    public enum BarkCategory
    {
        PlayerSpotted,
        Occupancy,
        PathPrediction,
        Idle,
        Confused
    }
}
