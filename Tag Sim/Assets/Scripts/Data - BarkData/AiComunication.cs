using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AiComunication : MonoBehaviour
{
    // Singleton instance
    public static AiComunication Instance { get; private set; }

    [Header("Communication Settings")]
    // Delay before acknowledgments appear after the initial spotting
    public float acknowledgmentDelay = 0.5f;

    // All AI bark controllers in the scene
    private List<AiBarkController> aiControllers = new List<AiBarkController>();

    // Track if communication is currently happening
    private bool isCommunicating = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Persist between scenes if needed
        // DontDestroyOnLoad(gameObject);
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
        AiBarkController[] controllers = FindObjectsOfType<AiBarkController>();

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

        StartCoroutine(SimultaneousCommunicationSequence(spotter));
    }

    /// <summary>
    /// Coroutine that manages the simultaneous AI responses
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
