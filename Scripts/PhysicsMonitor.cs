using UnityEngine;

public class PhysicsMonitor : MonoBehaviour
{
    [Header("Settings")]
    public bool autoCountAgents = true;
    public int agentCount = 10; // Set this to match your scene (16 or 8)
    public float pollingTime = 1.0f; // Update log every 2 seconds

    private float timeAccumulator = 0f;
    private float unscaledTimeAccumulator = 0f;
    private int physicsStepsAccumulator = 0;

    void Update()
    {
        // Accumulate time data
        timeAccumulator += Time.deltaTime;           // Game time (Simulated)
        unscaledTimeAccumulator += Time.unscaledDeltaTime; // Wall clock time (Real)

        if (unscaledTimeAccumulator > pollingTime)
        {
            if (autoCountAgents)
            {
                agentCount = FindObjectsOfType<CarAgent>().Length;
            }

            // 1. Calculate Effective Scale (Simulated Seconds / Real Seconds)
            float effectiveScale = timeAccumulator / unscaledTimeAccumulator;

            // 2. Calculate Physics Throughput (Total physics steps across all agents / sec)
            // Physics steps per real second = (PhysicsSteps / RealTime)
            float systemStepsPerSec = physicsStepsAccumulator / unscaledTimeAccumulator;
            
            // 3. Total Data Generation (The "Score")
            // Each agent generates 1 observation per FixedUpdate (or DecisionPeriod)
            // We use SystemSteps * AgentCount to see raw data volume.
            float totalDataThroughput = systemStepsPerSec * agentCount;

            // Formatting status
            string color = effectiveScale < (Time.timeScale * 0.8f) ? "yellow" : "green";
            if (effectiveScale < (Time.timeScale * 0.2f)) color = "red";

            Debug.Log($"<color={color}>[Performance] [{gameObject.name}] " +
                      $"Scale: {effectiveScale:F1}x (Req: {Time.timeScale:F0}x) | " +
                      $"FPS: {(1.0f / Time.unscaledDeltaTime):F0} | " +
                      $"Throughput: {totalDataThroughput:F0} steps/sec ({agentCount} agents)</color>");

            // Reset
            timeAccumulator = 0f;
            unscaledTimeAccumulator = 0f;
            physicsStepsAccumulator = 0;
        }
    }

    void FixedUpdate()
    {
        physicsStepsAccumulator++;
    }
}