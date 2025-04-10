using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AiComunication : MonoBehaviour
{
    public static AiComunication Instance { get; private set; }

    [Header("Communication Settings")]
    // Delay before acknowledgments appear after the initial spotting
    public float acknowledgmentDelay = 0.5f;

    public float lostPlayerReportCooldown = 5f;

    private List<AiBarkController> aiControllers = new List<AiBarkController>();
    private bool isCommunicating = false;
    private float lastLostPlayerReportTime = -9999f;
    private AiBarkController lastSpotter = null;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find all AI bark controllers in the scene
        FindAllAiControllers();
    }

    /// <summary>
    /// Finds and registers all AiBarkController components in the scene
    /// </summary>
    public void FindAllAiControllers()
    {
        aiControllers.Clear();
        AiBarkController[] controllers = FindObjectsByType<AiBarkController>(FindObjectsSortMode.None);

        foreach (AiBarkController controller in controllers)
        {
            RegisterAiController(controller);
        }

        Debug.Log($"AiCommunicationSystem registered {aiControllers.Count} AI controllers");
    }

    /// <summary>
    /// Registers a new AiBarkController with the system
    /// </summary>
    public void RegisterAiController(AiBarkController controller)
    {
        if (!aiControllers.Contains(controller))
        {
            aiControllers.Add(controller);
        }
    }

    /// <summary>
    /// Unregisters an AiBarkController from the system
    /// </summary>
    public void UnregisterAiController(AiBarkController controller)
    {
        if (aiControllers.Contains(controller))
        {
            aiControllers.Remove(controller);
        }
    }

    /// <summary>
    /// Called when an AI spots the player, initiating communication
    /// </summary>
    public void ReportPlayerSpotted(AiBarkController spotter)
    {
        // Don't start a new communication if one is already happening
        if (isCommunicating)
            return;

        // Remember who spotted the player
        lastSpotter = spotter;

        StartCoroutine(SimultaneousCommunicationSequence(spotter));
    }

    /// <summary>
    /// Called when the player's location is lost by all AIs.
    /// </summary>
    public void ReportPlayerLost()
    {
        // Only allow reports after the cooldown time has passed
        if (Time.time - lastLostPlayerReportTime < lostPlayerReportCooldown || isCommunicating)
            return;

        // Update the report time
        lastLostPlayerReportTime = Time.time;

        // Choose which AI will report the lost player
        AiBarkController reporter = ChoosePlayerLostReporter();

        if (reporter != null)
        {
            reporter.BarkPlayerLost();
        }
    }

    /// <summary>
    /// Chooses which AI should report the player being lost.
    /// </summary>
    private AiBarkController ChoosePlayerLostReporter()
    {
        // If the last spotter is still active, have them report
        if (lastSpotter != null && lastSpotter.gameObject.activeInHierarchy)
        {
            return lastSpotter;
        }

        // Otherwise, find all active AIs and pick one
        List<AiBarkController> activeControllers = new List<AiBarkController>();
        foreach (AiBarkController controller in aiControllers)
        {
            if (controller.gameObject.activeInHierarchy)
            {
                activeControllers.Add(controller);
            }
        }

        // If we have any active AIs, pick one randomly
        if (activeControllers.Count > 0)
        {
            return activeControllers[Random.Range(0, activeControllers.Count)];
        }

        // If no AIs are available, return null
        return null;
    }

    /// <summary>
    /// Coroutine that manages the simultaneous AI responses.
    /// </summary>
    private IEnumerator SimultaneousCommunicationSequence(AiBarkController spotter)
    {
        isCommunicating = true;

        // First, the spotter announces they've seen the player
        spotter.BarkPlayerSpotted();

        // Wait for a brief delay before all other AIs respond together
        yield return new WaitForSeconds(spotter.GetBarkDuration() + acknowledgmentDelay);

        // Create a list of AIs excluding the spotter
        List<AiBarkController> responders = new List<AiBarkController>(aiControllers);
        responders.Remove(spotter);

        // Have all AIs respond simultaneously
        foreach (AiBarkController responder in responders)
        {
            responder.BarkAcknowledge();
        }

        // Find the longest bark duration among responders to know when all are finished
        float longestBarkDuration = 0f;
        foreach (AiBarkController responder in responders)
        {
            float duration = responder.GetBarkDuration();
            if (duration > longestBarkDuration)
            {
                longestBarkDuration = duration;
            }
        }

        // Wait until all acknowledgments have finished
        yield return new WaitForSeconds(longestBarkDuration + 0.1f);

        isCommunicating = false;
    }
}
