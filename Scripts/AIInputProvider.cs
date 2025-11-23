using UnityEngine;

/// <summary>
/// Custom Input Provider for AI/ML-Agents control of NWH Vehicle Physics 2.
/// This provider allows the CarAgent to set vehicle inputs programmatically,
/// preventing conflicts with other input sources (keyboard/gamepad) that cause jittery behavior.
/// 
/// This is a standalone MonoBehaviour that doesn't require NWH base classes.
/// It stores input values with optional smoothing and applies them via the VehicleController.
/// 
/// Usage:
/// 1. Attach this component to your vehicle GameObject
/// 2. Remove or disable other Input Providers (InputManagerVehicleInputProvider, etc.)
/// 3. Set VehicleController.autoSetInput = false
/// 4. CarAgent will set the public input fields each frame
/// 5. This component will apply the smoothed values to the VehicleController
/// </summary>
public class AIInputProvider : MonoBehaviour
{
    [Header("AI Input Values")]
    [Tooltip("Steering input from AI (-1 to 1, where -1 is full left, 1 is full right)")]
    public float steeringInput = 0f;
    
    [Tooltip("Throttle input from AI (0 to 1, where 1 is full throttle)")]
    public float throttleInput = 0f;
    
    [Tooltip("Brake input from AI (0 to 1, where 1 is full brake)")]
    public float brakeInput = 0f;
    
    [Tooltip("Handbrake input from AI")]
    public bool handbrakeInput = false;

    [Header("Input Smoothing")]
    [Tooltip("Enable input smoothing to reduce jittery AI behavior")]
    public bool enableSmoothing = true;
    
    [Tooltip("Smoothing speed (higher = faster response, lower = smoother). Recommended: 5-15")]
    public float smoothingSpeed = 10f;

    // Internal smoothed values
    private float smoothedSteering = 0f;
    private float smoothedThrottle = 0f;
    private float smoothedBrake = 0f;

    [Header("Vehicle Controller Reference")]
    [Tooltip("Optional: Assign VehicleController here. If null, will try to find it automatically.")]
    public MonoBehaviour vehicleController;

    /// <summary>
    /// Gets the smoothed steering value (or raw if smoothing disabled).
    /// </summary>
    public float GetSteering()
    {
        if (enableSmoothing)
        {
            smoothedSteering = Mathf.Lerp(smoothedSteering, steeringInput, Time.fixedDeltaTime * smoothingSpeed);
            return smoothedSteering;
        }
        return steeringInput;
    }

    /// <summary>
    /// Gets the smoothed throttle value (or raw if smoothing disabled).
    /// </summary>
    public float GetThrottle()
    {
        if (enableSmoothing)
        {
            smoothedThrottle = Mathf.Lerp(smoothedThrottle, throttleInput, Time.fixedDeltaTime * smoothingSpeed);
            return smoothedThrottle;
        }
        return throttleInput;
    }

    /// <summary>
    /// Gets the smoothed brake value (or raw if smoothing disabled).
    /// </summary>
    public float GetBrake()
    {
        if (enableSmoothing)
        {
            smoothedBrake = Mathf.Lerp(smoothedBrake, brakeInput, Time.fixedDeltaTime * smoothingSpeed);
            return smoothedBrake;
        }
        return brakeInput;
    }

    /// <summary>
    /// Applies the current input values to the VehicleController.
    /// Called automatically in FixedUpdate, but can be called manually if needed.
    /// </summary>
    public void ApplyInputs()
    {
        if (vehicleController == null)
        {
            // Try to find VehicleController component (NWH Vehicle Physics 2)
            // Look for any MonoBehaviour with "VehicleController" in its type name
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp != null && comp != this && comp.GetType().Name.Contains("VehicleController"))
                {
                    vehicleController = comp;
                    break;
                }
            }
            if (vehicleController == null)
            {
                return; // No vehicle controller found - will try again next frame
            }
        }

        float steer = GetSteering();
        float throttle = GetThrottle();
        float brake = GetBrake();

        // Use reflection to set inputs (same approach as CarAgent fallback)
        var controllerType = vehicleController.GetType();
        var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Get the 'input' property/field
        object inputObject = null;
        var inputProp = controllerType.GetProperty("input", bindingFlags) 
                     ?? controllerType.GetProperty("Input", bindingFlags);
        
        if (inputProp != null)
        {
            inputObject = inputProp.GetValue(vehicleController);
        }
        else
        {
            var inputField = controllerType.GetField("input", bindingFlags)
                          ?? controllerType.GetField("Input", bindingFlags);
            if (inputField != null)
            {
                inputObject = inputField.GetValue(vehicleController);
            }
        }

        if (inputObject != null)
        {
            var inputType = inputObject.GetType();

            // Disable Auto Set Input
            var autoSetInputProp = inputType.GetProperty("autoSetInput", bindingFlags) 
                                ?? inputType.GetProperty("AutoSetInput", bindingFlags);
            if (autoSetInputProp != null && autoSetInputProp.CanWrite)
            {
                autoSetInputProp.SetValue(inputObject, false);
            }
            else
            {
                var autoSetInputField = inputType.GetField("autoSetInput", bindingFlags) 
                                     ?? inputType.GetField("AutoSetInput", bindingFlags);
                if (autoSetInputField != null)
                {
                    autoSetInputField.SetValue(inputObject, false);
                }
            }

            // Set Horizontal (Steering)
            var horizontalProp = inputType.GetProperty("Horizontal", bindingFlags) 
                              ?? inputType.GetProperty("horizontal", bindingFlags);
            if (horizontalProp != null && horizontalProp.CanWrite)
            {
                horizontalProp.SetValue(inputObject, steer);
            }

            // Set Vertical (Throttle/Brake combined)
            var verticalProp = inputType.GetProperty("Vertical", bindingFlags)
                            ?? inputType.GetProperty("vertical", bindingFlags);
            if (verticalProp != null && verticalProp.CanWrite)
            {
                float verticalInput = 0f;
                if (throttle > 0.01f) verticalInput = -throttle;
                else if (brake > 0.01f) verticalInput = brake;
                verticalProp.SetValue(inputObject, verticalInput);
            }
            else
            {
                // Fallback: separate properties
                var throttleProp = inputType.GetProperty("Throttle", bindingFlags);
                var brakeProp = inputType.GetProperty("Brakes", bindingFlags) 
                             ?? inputType.GetProperty("Brake", bindingFlags);
                if (throttleProp != null && throttleProp.CanWrite) throttleProp.SetValue(inputObject, throttle);
                if (brakeProp != null && brakeProp.CanWrite) brakeProp.SetValue(inputObject, brake);
            }
        }
    }

    void FixedUpdate()
    {
        // Automatically apply inputs every physics frame
        ApplyInputs();
    }

    /// <summary>
    /// Resets all inputs to zero. Call this at the start of each episode.
    /// </summary>
    public void ResetInputs()
    {
        steeringInput = 0f;
        throttleInput = 0f;
        brakeInput = 0f;
        handbrakeInput = false;
        
        smoothedSteering = 0f;
        smoothedThrottle = 0f;
        smoothedBrake = 0f;
    }
}

