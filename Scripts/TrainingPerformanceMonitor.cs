using UnityEngine;

/// <summary>
/// Monitors training performance, FPS, and actual simulation speedup.
/// Attach this to any GameObject in your scene (e.g., the Car).
/// </summary>
public class TrainingPerformanceMonitor : MonoBehaviour
{
    [Tooltip("How often to log performance stats (in seconds)")]
    public float logInterval = 2.0f;

    private float lastRealTime;
    private float lastGameTime;
    private int frameCount;
    
    void Start()
    {
        lastRealTime = Time.realtimeSinceStartup;
        lastGameTime = Time.time;
        frameCount = 0;
        
        Debug.Log($"[Performance] Monitor started. Target TimeScale: {Time.timeScale}");
        Debug.Log($"[Performance] Physics Settings: FixedDeltaTime={Time.fixedDeltaTime}, MaxAllowedTimestep={Time.maximumDeltaTime}");
    }

    void Update()
    {
        frameCount++;
        float realDelta = Time.realtimeSinceStartup - lastRealTime;

        // Check interval
        if (realDelta >= logInterval)
        {
            float gameDelta = Time.time - lastGameTime;
            
            // Calculate metrics
            float actualSpeedup = gameDelta / realDelta;
            float fps = frameCount / realDelta;
            float targetSpeed = Time.timeScale;
            
            // Analyze CPU limitation
            // If actual speed is significantly less than target (e.g. < 85%), we are throttled
            bool isCpuLimited = actualSpeedup < (targetSpeed * 0.85f);
            string status = isCpuLimited ? "⚠️ CPU LIMITED" : "✅ OK";
            
            Debug.Log($"[Performance] {status} | Speed: {actualSpeedup:F1}x (Target: {targetSpeed:F0}x) | FPS: {fps:F0} | FrameTime: {(1000f/fps):F1}ms");
            
            // Reset
            lastRealTime = Time.realtimeSinceStartup;
            lastGameTime = Time.time;
            frameCount = 0;
        }
    }
}

