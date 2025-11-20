using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Reflection;

public class CarAgent : Agent
{
    [Header("Sensors & Vehicle")]
    public RaycastSensor ray;            // your RaycastSensor component
    public Rigidbody rb;                 // car rigidbody

    // If you drive NWH Vehicle Physics 2, drag its VehicleController here
    public MonoBehaviour nwhVehicleController; 

    public NwhWheelTelemetry wheelTelemetry; // drag in Inspector

    [Header("Actuation")]
    public float steerScale = 1f;

    [Header("Reward Settings")]
    [Tooltip("Reward per m/s of forward speed")]
    public float speedRewardMultiplier = 0.01f;
    [Tooltip("Reward for staying on track per step")]
    public float onTrackReward = 0.01f;
    [Tooltip("Penalty for excessive lateral slip")]
    public float slipPenaltyMultiplier = 0.1f;
    [Tooltip("Reward for maintaining good traction (low slip)")]
    public float tractionRewardMultiplier = 0.05f;
    [Tooltip("Maximum slip value to receive traction reward (normalized)")]
    public float goodTractionSlipThreshold = 0.3f;
    [Tooltip("Penalty for wheel lock/spin")]
    public float wheelEventPenalty = 0.2f;
    [Tooltip("Penalty for getting too close to obstacles")]
    public float obstaclePenaltyMultiplier = 0.5f;
    [Tooltip("Minimum normalized distance before penalty (0-1)")]
    public float obstacleThreshold = 0.2f;
    [Tooltip("Reward for forward progress (velocity in forward direction)")]
    public float forwardProgressMultiplier = 0.02f;
    [Tooltip("Reward for being grounded (all wheels on ground)")]
    public float groundedReward = 0.01f;
    [Tooltip("Penalty for not being grounded (car in air)")]
    public float notGroundedPenalty = 0.1f;

    [Header("Episode Termination")]
    [Tooltip("End episode if car goes off track")]
    public float endOnOffTrack = 1f;  // TO DO:to implement
    [Tooltip("End episode if car crashes (collision-based detection)")]
    public bool endOnCrash = true;
    [Tooltip("Minimum speed to be considered moving (m/s)")]
    public float minSpeedForProgress = 0.5f;

    [Header("Debug Logging")]
    [Tooltip("Enable debug logging for actions and environment")]
    public bool enableDebugLogging = true;
    [Tooltip("Log every N frames (0 = every frame, 50 = every 50 frames)")]
    public int logFrequency = 50;

    private CheckpointManager checkpointManager;
    private Vector3 lastPosition;
    private float episodeStartTime;
    private int actionCount = 0;
    private string lastInputMethod = "None";
    private bool hasCrashed = false; // Track if car has crashed via collision
    private Vector3 spawnPosition; // Initial spawn position
    private Quaternion spawnRotation; // Initial spawn rotation

    public override void Initialize()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        checkpointManager = FindObjectOfType<CheckpointManager>();
        lastPosition = transform.position;
        
        // Store initial spawn position and rotation for episode resets
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        
        // Disable Auto Set Input on NWH Vehicle Controller to allow agent control
        // DisableAutoSetInput();
        
        if (enableDebugLogging)
        {
            Debug.Log($"[CarAgent] Initialize - Agent initialized. Rigidbody: {(rb != null ? "OK" : "MISSING")}, WheelTelemetry: {(wheelTelemetry != null ? "OK" : "MISSING")}, RaySensor: {(ray != null ? "OK" : "MISSING")}");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        int observationCount = 0;
        
        // Rays (0..1 normalized)
        int rayCount = 0;
        if (ray && ray.Distances != null)
        {
            rayCount = ray.Distances.Length;
            foreach (var d in ray.Distances) 
            {
                sensor.AddObservation(d);
                observationCount++;
            }
        }
        else
        {
            rayCount = 13;
            sensor.AddObservation(new float[13]); // keep shape stable if missing
            observationCount += 13;
        }

        // Speed (normalize by e.g. 80 m/s = 288 km/h)
        float speed = rb ? rb.linearVelocity.magnitude : 0f;
        sensor.AddObservation(Mathf.Clamp01(speed / 80f));
        observationCount++;

        // Yaw rate (normalize around ±5 rad/s)
        float yaw = rb ? transform.InverseTransformVector(rb.angularVelocity).y : 0f;
        sensor.AddObservation(Mathf.Clamp(yaw / 5f, -1f, 1f));
        observationCount++;

        // Slips
        sensor.AddObservation(Mathf.Clamp01(Mathf.Abs(wheelTelemetry.maxAbsLatSlip) / 1.5f));
        observationCount++;
        sensor.AddObservation(Mathf.Clamp01(Mathf.Abs(wheelTelemetry.maxAbsLongSlip) / 1.5f));
        observationCount++;

        // Events / track
        sensor.AddObservation(wheelTelemetry.anyWheelLocked   ? 1f : 0f);
        observationCount++;
        sensor.AddObservation(wheelTelemetry.anyWheelSpinning ? 1f : 0f);
        observationCount++;
        sensor.AddObservation(wheelTelemetry.WheelsOnTrack ? 1f : 0f);
        observationCount++;

        // Torques (normalized)
        sensor.AddObservation(wheelTelemetry.normMotorTorque); // [0..1]
        observationCount++;
        sensor.AddObservation(wheelTelemetry.normBrakeTorque); // [0..1]
        observationCount++;
        
        // Log observation count breakdown (only once per episode to avoid spam)
        if (enableDebugLogging && actionCount == 0)
        {
            int otherObs = observationCount - rayCount;
            Debug.Log($"[CarAgent] Observation breakdown: Total={observationCount} (Rays={rayCount}, Speed=1, YawRate=1, Slips=2, WheelStates=3, Torques=2, Other={otherObs}). " +
                     $"Expected: {rayCount}+9={rayCount+9}. " +
                     $"Update Behavior Parameters > Vector Observation Space Size to {observationCount} to fix the warning.");
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        actionCount++;
        
        // Get raw actions from the agent
        float rawSteer = actions.ContinuousActions[0];
        float rawAccelAxis = actions.ContinuousActions[1];
        if (enableDebugLogging)
        {
            Debug.Log($"[CarAgent] Action #{actionCount} - Raw: [Steer={rawSteer:F3}]");
            Debug.Log($"[CarAgent] Action #{actionCount} - Raw: [Accel={rawAccelAxis:F3}]");
        }
        // Process actions
        float steer = Mathf.Clamp(rawSteer, -1f, 1f) * steerScale;
        float accelAxis = Mathf.Clamp(rawAccelAxis, -1f, 1f);
        float throttle = Mathf.Clamp01(accelAxis);
        float brake    = Mathf.Clamp01(-accelAxis);

        // Debug logging
        if (enableDebugLogging && (logFrequency == 0 || actionCount % logFrequency == 0))
        {
            Debug.Log($"[CarAgent] Action #{actionCount} - Raw: [Steer={rawSteer:F3}, Accel={rawAccelAxis:F3}] | Processed: [Steer={steer:F3}, Throttle={throttle:F3}, Brake={brake:F3}] | Method: {lastInputMethod}");
        }

        // Apply actions to vehicle control
        ApplyVehicleInputs(steer, throttle, brake);

        // Calculate and apply rewards
        CalculateRewards();

        // Check for episode termination conditions
        CheckTerminationConditions();
    }

    /// <summary>
    /// Applies vehicle inputs (steering, throttle, brake) to the vehicle controller.
    /// Uses NWH Vehicle Physics 2 input API as documented: https://nwhvehiclephysics.com/doku.php/Setup/Input
    /// Documentation: vehicleController.input.Horizontal, vehicleController.input.Throttle, vehicleController.input.Brakes
    /// Or: vehicleController.input.states.horizontal, vehicleController.input.states.throttle, vehicleController.input.states.brakes
    /// </summary>
    /// <param name="steer">Steering input (-1 to 1)</param>
    /// <param name="throttle">Throttle input (0 to 1)</param>
    /// <param name="brake">Brake input (0 to 1)</param>
    void ApplyVehicleInputs(float steer, float throttle, float brake)
    {
        bool inputApplied = false;

        // Method 1: Try NWH Vehicle Physics 2 controller (if assigned)
        if (nwhVehicleController != null)
        {
            var controllerType = nwhVehicleController.GetType();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            // Get the 'input' property from the vehicle controller
            object inputObject = null;
            var inputProp = controllerType.GetProperty("input", bindingFlags) 
                         ?? controllerType.GetProperty("Input", bindingFlags);
            
            if (inputProp != null)
            {
                inputObject = inputProp.GetValue(nwhVehicleController);
            }
            // Fallback to field if property missing
            else
            {
                var inputField = controllerType.GetField("input", bindingFlags)
                              ?? controllerType.GetField("Input", bindingFlags);
                if (inputField != null)
                {
                    inputObject = inputField.GetValue(nwhVehicleController);
                }
            }
            
            if (inputObject != null)
            {
                var inputType = inputObject.GetType();

                // --- STEP 1: Disable Auto Set Input (Crucial!) ---
                // vehicleController.input.autoSetInput = false;
                var autoSetInputProp = inputType.GetProperty("autoSetInput", bindingFlags) 
                                    ?? inputType.GetProperty("AutoSetInput", bindingFlags);
                
                if (autoSetInputProp != null && autoSetInputProp.CanWrite)
                {
                    autoSetInputProp.SetValue(inputObject, false);
                }
                else
                {
                    // Try field
                    var autoSetInputField = inputType.GetField("autoSetInput", bindingFlags) 
                                         ?? inputType.GetField("AutoSetInput", bindingFlags);
                    if (autoSetInputField != null)
                    {
                        autoSetInputField.SetValue(inputObject, false);
                    }
                }

                // var throttleProp = inputType.GetProperty("Throttle", bindingFlags) 
                //                 ?? inputType.GetProperty("throttle", bindingFlags);
                                
                // var brakeProp = inputType.GetProperty("Brakes", bindingFlags)
                //              ?? inputType.GetProperty("brakes", bindingFlags);
                
                // --- STEP 2: Apply Inputs via Properties (Horizontal, Vertical) ---
                // Documentation says: vehicleController.input.Horizontal = val; 
                // And: vehicleController.input.Vertical = 0.5f (throttle), -0.5f (brake)
                
                // Horizontal (Steering)
                var horizontalProp = inputType.GetProperty("Horizontal", bindingFlags) 
                                  ?? inputType.GetProperty("horizontal", bindingFlags)
                                  ?? inputType.GetProperty("Steering", bindingFlags); // Fallback
                
                if (horizontalProp != null && horizontalProp.CanWrite)
                {
                    horizontalProp.SetValue(inputObject, steer);
                    // horizontalProp.SetValue(nwhVehicleController, steer); // This is the correct way to set the steering
                    inputApplied = true;
                }

                // Vertical (Throttle/Brake Combined)
                // Positive = Throttle, Negative = Brake
                var verticalProp = inputType.GetProperty("Vertical", bindingFlags)
                                ?? inputType.GetProperty("vertical", bindingFlags);

                if (verticalProp != null && verticalProp.CanWrite)
                {
                    // Combine throttle (0..1) and brake (0..1) into one Vertical axis (-1..1)
                    float verticalInput = 0f;
                    if (throttle > 0.01f) verticalInput = throttle;
                    else if (brake > 0.01f) verticalInput = -brake;

                    verticalProp.SetValue(inputObject, verticalInput);
                    // inputApplied = true; // Vertical counts as application
                }
                else
                {
                    // Fallback: separate properties if Vertical doesn't exist
                    var throttleProp = inputType.GetProperty("Throttle", bindingFlags);
                    var brakeProp = inputType.GetProperty("Brakes", bindingFlags) 
                                 ?? inputType.GetProperty("Brake", bindingFlags);

                    if (throttleProp != null && throttleProp.CanWrite) throttleProp.SetValue(inputObject, throttle);
                    if (brakeProp != null && brakeProp.CanWrite) brakeProp.SetValue(inputObject, brake);
                }

                if (inputApplied)
                {
                    lastInputMethod = "NWH Vehicle Controller (via Properties)";
                    return; 
                }
            }
        } 


        // Warn if no input method worked
        if (!inputApplied && enableDebugLogging && (logFrequency == 0 || actionCount % logFrequency == 0))
        {
            Debug.LogWarning($"[CarAgent] WARNING: No input method applied! Check vehicle controller setup. " +
                           $"nwhVehicleController: {(nwhVehicleController != null ? "ASSIGNED" : "NULL")}, " +
                           $"wheelTelemetry: {(wheelTelemetry != null ? "ASSIGNED" : "NULL")}, " +
                           $"rb: {(rb != null ? "ASSIGNED" : "NULL")}. " +
                           $"Steer: {steer:F3}, Throttle: {throttle:F3}, Brake: {brake:F3}");
        }
    }

    void CalculateRewards()
    {
        if (!rb || !wheelTelemetry) return;

        float speed = rb.linearVelocity.magnitude;
        Vector3 forwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);
        float forwardSpeed = forwardVelocity.magnitude * Mathf.Sign(Vector3.Dot(forwardVelocity, transform.forward));

        // 1. Speed reward (encourage forward movement)
        if (forwardSpeed > minSpeedForProgress)
        {
            AddReward(forwardSpeed * speedRewardMultiplier);
        }

        // 2. Forward progress reward (encourage moving forward, not backward)
        if (forwardSpeed > 0)
        {
            AddReward(forwardSpeed * forwardProgressMultiplier);
        }

        // 3. On-track reward
        if (wheelTelemetry.WheelsOnTrack)
        {
            AddReward(onTrackReward);
        } else {
            AddReward(-1f); // Penalty for going off track
        }

        // 4. Traction rewards/penalties
        float totalSlip = Mathf.Abs(wheelTelemetry.maxAbsLatSlip) + Mathf.Abs(wheelTelemetry.maxAbsLongSlip);
        
        // Penalty for excessive slipping
        float slipPenalty = totalSlip * slipPenaltyMultiplier;
        AddReward(-slipPenalty);
        
        // Positive reward for maintaining good traction (only when moving)
        if (forwardSpeed > minSpeedForProgress && totalSlip < goodTractionSlipThreshold)
        {
            // Reward increases as slip decreases (better traction = more reward)
            float tractionQuality = 1f - (totalSlip / goodTractionSlipThreshold);
            AddReward(tractionQuality * tractionRewardMultiplier);
        }

        // 5. Wheel event penalties (lock/spin)
        if (wheelTelemetry.anyWheelLocked)
        {
            AddReward(-wheelEventPenalty);
        }
        if (wheelTelemetry.anyWheelSpinning)
        {
            AddReward(-wheelEventPenalty * 0.5f); // spinning is less bad than locking
        }

        // 6. Obstacle avoidance (penalize getting too close to obstacles)
        if (ray && ray.Distances != null)
        {
            float minDistance = 0.5f;
            foreach (var dist in ray.Distances)
            {
                minDistance = Mathf.Min(minDistance, dist);
            }

            if (minDistance < obstacleThreshold)
            {
                float penalty = (obstacleThreshold - minDistance) / obstacleThreshold * obstaclePenaltyMultiplier;
                AddReward(-penalty);
            }
        }

        // 7. Grounded reward/penalty
        if (wheelTelemetry.isGrounded)
        {
            AddReward(groundedReward);
        }
        else
        {
            AddReward(-notGroundedPenalty);
        }

        lastPosition = transform.position;
    }

    void CheckTerminationConditions()
    {
        if (!rb || !wheelTelemetry) return;

        // Check for crash (collision-based detection)
        if (endOnCrash && hasCrashed)
        {
            AddReward(-1f); // Large penalty for crashing
            EndEpisode();
            return;
        }
    }

    // Detect crashes via collision events
    void OnCollisionEnter(Collision collision)
    {
        // Filter out collisions with the Track or Road
        // If wheelTelemetry is available, use its track mask
        if (wheelTelemetry != null)
        {
            if ((wheelTelemetry.trackSurfaceMask.value & (1 << collision.gameObject.layer)) != 0)
            {
                return; // Hit the track surface, ignore
            }
        }
        
        // Also ignore if tag is "Road" or layer is "TrackSurface" as a backup
        if (collision.gameObject.CompareTag("Road") || collision.gameObject.layer == LayerMask.NameToLayer("TrackSurface"))
        {
            return;
        }

        // Debug log for ANY other collision to help diagnosis
        Debug.Log($"[CarAgent] Collision detected with: {collision.gameObject.name} (Tag: {collision.gameObject.tag}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");

        // Check if collision is significant enough to be considered a crash
        if (endOnCrash)
        {
            hasCrashed = true;
            
            if (enableDebugLogging)
            {
                Debug.Log($"[CarAgent] CRASH CONFIRMED! Requesting episode reset.");
            }

            // Force episode termination immediately
            AddReward(-1f);
            EndEpisode();
        }
    }

    // Callback for checkpoint system
    public void OnCheckpointHit(bool correct, int checkpointIndex, int lap)
    {
        if (correct)
        {
            AddReward(0.2f); // Reward for hitting correct checkpoint
            if (lap > 0)
            {
                AddReward(1f); // Bonus for completing a lap
            }
        }
        else
        {
            AddReward(-0.3f); // Penalty for wrong checkpoint
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        
        // Initialize inputs
        float steer = 0f;
        float throttleBrake = 0f;

        // 1. Keyboard Input
        if (Keyboard.current != null)
        {
            // Steering
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) 
                steer = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) 
                steer = 1f;

            // Throttle / Brake
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) 
                throttleBrake = 1f;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) 
                throttleBrake = -1f;
        }

        // 2. Gamepad Input (overrides keyboard if present)
        if (Gamepad.current != null)
        {
            // Steering (Left Stick X)
            float stickX = Gamepad.current.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) > 0.1f) // deadzone
            {
                steer = stickX;
            }

            // Throttle (Right Trigger) & Brake (Left Trigger)
            float rightTrigger = Gamepad.current.rightTrigger.ReadValue();
            float leftTrigger = Gamepad.current.leftTrigger.ReadValue();
            
            if (rightTrigger > 0.05f)
            {
                throttleBrake = rightTrigger;
            }
            else if (leftTrigger > 0.05f)
            {
                throttleBrake = -leftTrigger;
            }
        }

        // Assign to actions
        ca[0] = steer;
        ca[1] = throttleBrake;
    }

    public override void OnEpisodeBegin()
    {
        // Reset position and rotation to spawn point
        if (rb)
        {
            // Reset velocities first
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Reset position and rotation using Rigidbody methods (proper for physics objects)
            rb.MovePosition(spawnPosition);
            rb.MoveRotation(spawnRotation);
            
            // Also set transform directly to ensure it's synced
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }
        else
        {
            // Fallback if no rigidbody
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }
        
        lastPosition = spawnPosition;
        episodeStartTime = Time.time;
        actionCount = 0;
        hasCrashed = false; // Reset crash flag for new episode
        
        // Reset vehicle controller inputs to zero (if using Vehicle Controller)
        if (nwhVehicleController != null)
        {
            var controllerType = nwhVehicleController.GetType();
            var steeringProp = controllerType.GetProperty("Steering") ?? controllerType.GetProperty("steering");
            var throttleProp = controllerType.GetProperty("Throttle") ?? controllerType.GetProperty("throttle");
            var brakeProp = controllerType.GetProperty("Brakes") 
                         ?? controllerType.GetProperty("brakes")
                         ?? controllerType.GetProperty("Brake")
                         ?? controllerType.GetProperty("brake");
            
            if (steeringProp != null && steeringProp.CanWrite) steeringProp.SetValue(nwhVehicleController, 0f);
            if (throttleProp != null && throttleProp.CanWrite) throttleProp.SetValue(nwhVehicleController, 0f);
            if (brakeProp != null && brakeProp.CanWrite) brakeProp.SetValue(nwhVehicleController, 0f);
        }
        
        // Reset wheel controller inputs to zero (if using direct wheel control)
        if (wheelTelemetry != null)
        {
            if (wheelTelemetry.FL != null)
            {
                wheelTelemetry.FL.SteerAngle = 0f;
                wheelTelemetry.FL.MotorTorque = 0f;
                wheelTelemetry.FL.BrakeTorque = 0f;
            }
            if (wheelTelemetry.FR != null)
            {
                wheelTelemetry.FR.SteerAngle = 0f;
                wheelTelemetry.FR.MotorTorque = 0f;
                wheelTelemetry.FR.BrakeTorque = 0f;
            }
            if (wheelTelemetry.RL != null)
            {
                wheelTelemetry.RL.MotorTorque = 0f;
                wheelTelemetry.RL.BrakeTorque = 0f;
            }
            if (wheelTelemetry.RR != null)
            {
                wheelTelemetry.RR.MotorTorque = 0f;
                wheelTelemetry.RR.BrakeTorque = 0f;
            }
        }
        
        // Reset checkpoint state for new episode
        if (checkpointManager != null)
        {
            checkpointManager.ResetAgentState(this);
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"[CarAgent] OnEpisodeBegin - Episode started. Reset to spawn position: {spawnPosition}, Rotation: {spawnRotation.eulerAngles}. Velocity reset. Action count reset to 0. Checkpoint state reset.");
        }
    }

    void FixedUpdate()
    {
        // NOTE: DecisionRequester component should be added to the car GameObject in Unity Inspector
        // It will automatically call RequestDecision() at the configured interval.
        // If DecisionRequester is not present, uncomment the line below:
        // RequestDecision();
        
        // Diagnostic: Log if agent is running but not receiving actions
        if (enableDebugLogging && Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
        {
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            Debug.Log($"[CarAgent] Status - Frame: {Time.frameCount}, Episode: {GetCumulativeReward():F2}, Speed: {speed:F2} m/s, Actions received: {actionCount}, Input method: {lastInputMethod}");
            
            // Warning if no actions received
            if (actionCount == 0 && Time.frameCount > 300)
            {
                Debug.LogWarning($"[CarAgent] WARNING: No actions received after {Time.frameCount} frames! " +
                    "Check: 1) Behavior Type = 'Default', 2) Decision Requester added, 3) Training script connected");
            }
        }
    }
}
