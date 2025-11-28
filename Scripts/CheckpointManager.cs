using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Header("Assign the parent that contains all CheckpointTrigger children")]
    public Transform checkpointsParent;      // drag your "Checkpoints" object here
    // public float minForwardSpeed = 0.5f;     // m/s: ignore tiny bumps
    [Range(0f,1f)] public float minForwardDot = 0.1f; // require crossing roughly in forward dir

    [HideInInspector] public List<CheckpointTrigger> Checkpoints = new();

    class AgentState 
    { 
        public int last = -1; 
        public int lap = 0; 
        public float lastHitTime = 0f; // Time when last checkpoint was hit
        public int lastHitIndex = -1;    // Index of last checkpoint hit
    }
    readonly Dictionary<CarAgent, AgentState> states = new();
    
    [Header("Anti-Spam Settings")]
    [Tooltip("Minimum time (seconds) between checkpoint hits to prevent double-triggering")]
    public float checkpointCooldown = 0.5f;

    void OnValidate() { RefreshList(); }
    void Awake()      { RefreshList(); }

    public void RefreshList()
    {
        if (!checkpointsParent) checkpointsParent = transform;
        Checkpoints.Clear();
        checkpointsParent.GetComponentsInChildren(true, Checkpoints);

        // Auto-number in hierarchy order if Index left at -1
        for (int i = 0; i < Checkpoints.Count; i++)
            if (Checkpoints[i] && Checkpoints[i].Index < 0) Checkpoints[i].Index = i;
    }

    /// Call this from a thin trigger forwarder on each checkpoint
    public void ReportHit(CarAgent agent, int index, Vector3 velocity, Vector3 cpForward)
    {
        // Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) processing hit {index} for {agent.name}");
        if (agent == null || Checkpoints.Count == 0) return;

        if (!states.TryGetValue(agent, out var s)) states[agent] = s = new AgentState();
        // Debug.Log($"[CP] Agent State: last={s.last}, lap={s.lap}, lastHitIndex={s.lastHitIndex}, lastHitTime={s.lastHitTime}");

        // Anti-spam: Prevent same checkpoint from triggering multiple times in quick succession
        float currentTime = Time.time;
        if (s.lastHitIndex == index && (currentTime - s.lastHitTime) < checkpointCooldown)
        {
            // Same checkpoint triggered too soon - ignore
            return;
        }

        // if (velocity.magnitude < minForwardSpeed) return;
        if (Vector3.Dot(cpForward, velocity.normalized) < minForwardDot) return;

        int count = Checkpoints.Count;
        
        // FIX: Handle first checkpoint correctly when starting
        // If last is -1 (initial state), expect checkpoint 0
        int expected;
        if (s.last == -1)
        {
            expected = 0; // First checkpoint should be index 0
        }
        else
        {
            expected = (s.last + 1) % count; // Next checkpoint in sequence
        }

        if (index == expected)
        {
            bool wrapped = (s.last == count - 1 && index == 0);
            s.last = index;
            s.lastHitIndex = index;
            s.lastHitTime = currentTime;
            if (wrapped) s.lap++;

            Debug.Log($"[CP] {agent.name}: OK -> {index}, lap={s.lap}");
            agent.OnCheckpointHit(true, index, s.lap);
        }
        else if (index == s.last)
        {
            // repeat, ignore (but allow first checkpoint if last is -1)
            if (s.last == -1 && index == 0)
            {
                // This shouldn't happen, but handle it just in case
                s.last = index;
                Debug.Log($"[CP] {agent.name}: First checkpoint {index} (initialized)");
                agent.OnCheckpointHit(true, index, s.lap);
            }
            else
            {
                Debug.Log($"[CP] {agent.name}: repeat {index} (ignored)");
            }
        }
        else
        {
            Debug.LogWarning($"[CP] {agent.name}: WRONG -> {index} (expected {expected}, last was {s.last})");
            agent.OnCheckpointHit(false, index, s.lap);
            // Optional: agent.EndEpisode();
        }
    }

    /// Reset checkpoint state for an agent (call from OnEpisodeBegin)
     public void ResetAgentState(CarAgent agent)
    {   
        Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) resetting state for {agent.name}");
        if (!states.TryGetValue(agent, out var s))
        {
            s = new AgentState();
            states[agent] = s;
            // Debug.Log($"[CP] New state created for agent {agent.name}");
        }
        
        if (agent != null && states.ContainsKey(agent))
        {
            s.last = -1;
            s.lap = 0;
            s.lastHitTime = 0f;
            s.lastHitIndex = -1;
            // Debug.Log($"[CP] State reset for agent {agent.name}");
        }
        
    }
}
