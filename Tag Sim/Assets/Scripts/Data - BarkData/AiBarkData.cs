using UnityEngine;

[CreateAssetMenu(fileName = "AiBarkData", menuName = "Scriptable Objects/AiBarkData")]
public class AiBarkData : ScriptableObject
{
    [Header("When the AI sees the player")]
    public string[] playerSpottedLines;

    [Header("For occupancy map / area evaluation events")]
    public string[] occupancyBarkLines;

    [Header("For path prediction / target locking events")]
    public string[] pathPredictionLines;

    // You can add additional arrays here if needed.
}
