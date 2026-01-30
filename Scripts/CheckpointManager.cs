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
        public float lastHitTime = -1f; // Time when last checkpoint was hit
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
        if (agent == null || Checkpoints.Count == 0) 
        {
             Debug.Log($"[CP] Hit {index} ignored: Agent null or checkpoints empty");
             return;
        }

        if (!states.TryGetValue(agent, out var s)) states[agent] = s = new AgentState();
        // Debug.Log($"[CP] Agent State: last={s.last}, lap={s.lap}, lastHitIndex={s.lastHitIndex}, lastHitTime={s.lastHitTime}");

        // Anti-spam: Prevent same checkpoint from triggering multiple times in quick succession
        float currentTime = Time.time;
        if (s.lastHitIndex == index && (currentTime - s.lastHitTime) < checkpointCooldown)
        {
            // Same checkpoint triggered too soon - ignore
            // Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) {agent.name}: Ignored hit {index} due to cooldown/spam (LastHit={s.lastHitIndex} at {s.lastHitTime}, Now={currentTime})");
            return;
        }

        // if (velocity.magnitude < minForwardSpeed) return;
        float dot = Vector3.Dot(cpForward, velocity.normalized);
        // Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) Dot Product Check: Index={index}, Velocity={velocity}, Normalized={velocity.normalized}, CP_Fwd={cpForward}, Dot={dot}, Min={minForwardDot}");

        if (dot < minForwardDot)
        {
            // Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) {agent.name}: Ignored hit {index} due to angle (Dot={dot:F3} < {minForwardDot})");
            return;
        }
        
        // Debug.Log($"[CP] Trace: Passed Angle Check. Entering Logic Block...");

        // Paranoid check + Try-Catch to find the silent killer
        try 
        {
            // Debug.Log($"[CP] Trace: Accessing Checkpoints.Count...");
            int count = Checkpoints.Count;
            // Debug.Log($"[CP] Trace: CheckpointsCount={count}");
            
            if (count == 0) 
            {
                Debug.LogError($"[CP] MANAGER ({this.GetInstanceID()}) FATAL: Checkpoints.Count is 0 inside logic block! This should be impossible due to early check.");
                return;
            }

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
            
            // Debug.Log($"[CP] Logic Check: Index={index}, Last={s.last}, Expected={expected}, Lap={s.lap}, Count={count}");

            if (index == expected)
            {
                bool wrapped = (s.last == count - 1 && index == 0);

                //Check if we are just starting the race (No CP -> CP 0)
                bool firstStart = (s.last == -1 && index == 0);

                s.last = index;
                s.lastHitIndex = index;
                s.lastHitTime = currentTime;

                // Increment lap on EITHER a full loop OR the first start
                if (wrapped || firstStart) 
                {
                    s.lap++;
                }

                // Debug.Log($"[CP] {agent.name}: OK -> {index}, lap={s.lap}");
                agent.OnCheckpointHit(true, index, s.lap);
            }
            else
            {
                // 1. Update the Anti-Spam memory so we don't punish 50 times per second
                s.lastHitIndex = index;       
                s.lastHitTime = currentTime;

                // 2. Log exactly WHY it failed (crucial for debugging Part 2)
                Debug.LogWarning($"[CP] MANAGER ({this.GetInstanceID()}) WRONG Hit! Agent hit Index {index}, but Manager expected {expected}. (Last correct was {s.last})");

                agent.OnCheckpointHit(false, index, s.lap);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CP] MANAGER ({this.GetInstanceID()}) EXCEPTION processing hit {index}: {e}");
        }
    }

    public List<Transform> GetNextCheckpoints(CarAgent agent, int count = 3)
    {
        List<Transform> nextCheckpoints = new List<Transform>();
        if (!states.TryGetValue(agent, out var s) || Checkpoints.Count == 0) return nextCheckpoints;

        for (int i = 1; i <= count; i++)
        {
            int nextIndex = (s.last + i) % Checkpoints.Count;
            nextCheckpoints.Add(Checkpoints[nextIndex].transform);
        }
        return nextCheckpoints;
    }

    /// Reset checkpoint state for an agent (call from OnEpisodeBegin)
    public void ResetAgentState(CarAgent agent)
    {   
        // Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) resetting state for {agent.name}");
        if (!states.TryGetValue(agent, out var s))
        {
            s = new AgentState();
            states[agent] = s;
            // Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) tate created for agent {agent.name} during reset");
        }
        
        if (s != null) // simplified check
        {
            s.last = -1;
            s.lap = 0;
            s.lastHitTime = -1f;
            s.lastHitIndex = -1;
            Debug.Log($"[CP] MANAGER ({this.GetInstanceID()}) State reset for {agent.name}: Last={s.last}, Lap={s.lap}, LastHitTime={s.lastHitTime}");
        }
    }
}
