# Troubleshooting: Agent Not Providing Inputs

## Problem
The agent receives actions from the neural network but the car doesn't move.

## Root Cause
The `OnActionReceived` method in `CarAgent.cs` was calculating actions (steer, throttle, brake) but **not applying them** to the vehicle controller. The code had TODO comments instead of actual implementation.

## Solution Applied
I've implemented the `ApplyVehicleInputs()` method that tries three approaches:

1. **NWH Vehicle Physics 2 Controller** (if assigned in Inspector)
2. **Direct NWH WheelController3D** control (if wheel telemetry is set up)
3. **Rigidbody fallback** (simple force-based control)

## How to Verify It's Working

### Step 1: Check Unity Inspector
1. Select your car GameObject with `CarAgent` component
2. Verify these are assigned:
   - ✅ `RaycastSensor` component
   - ✅ `Rigidbody` component
   - ✅ `NwhWheelTelemetry` component
   - ✅ `Wheel Telemetry` → FL, FR, RL, RR wheels assigned
   - ⚠️ `Nwh Vehicle Controller` (optional, if using NWH VP2)

### Step 2: Check Behavior Parameters
1. Find `Behavior Parameters` component on the car
2. Verify:
   - ✅ `Behavior Name`: `CarAgentParams` (matches config file)
   - ✅ `Behavior Type`: `Default` (for training) or `Inference Only` (for trained model)
   - ✅ `Vector Observation > Space Size`: Should match your observations (~22)
   - ✅ `Actions > Continuous Actions > Space Size`: `2` (steer, throttle/brake)

### Step 3: Check Decision Requester
1. Add `Decision Requester` component to the car (if not present)
2. Settings:
   - `Decision Period`: `1` (decision every step)
   - `Take Actions Between Decisions`: `true`

### Step 4: Test with Heuristic (Manual Control)
1. In `Behavior Parameters`, set `Behavior Type` to `Heuristic Only`
2. Press Play in Unity
3. Use arrow keys or WASD to control the car
4. If this works, the vehicle controller is set up correctly

### Step 5: Check Training Connection
1. Start Unity and press Play
2. Start training script (`.\train_model.ps1`)
3. Check Unity Console for:
   - "Connected new brain: CarAgentParams"
   - No errors about missing components

### Step 6: Add Debug Logging
Add this to `OnActionReceived` to verify actions are being received:

```csharp
public override void OnActionReceived(ActionBuffers actions)
{
    float steer = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f) * steerScale;
    float accelAxis = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
    float throttle = Mathf.Clamp01(accelAxis);
    float brake    = Mathf.Clamp01(-accelAxis);

    // DEBUG: Log actions (remove after testing)
    if (Time.frameCount % 50 == 0) // Log every 50 frames
    {
        Debug.Log($"Actions - Steer: {steer:F2}, Throttle: {throttle:F2}, Brake: {brake:F2}");
    }

    ApplyVehicleInputs(steer, throttle, brake);
    // ... rest of code
}
```

## Common Issues

### Issue 1: Car Doesn't Move at All
**Possible causes:**
- Wheel controllers not assigned in `NwhWheelTelemetry`
- Vehicle controller not set up correctly
- Rigidbody constraints preventing movement

**Solution:**
- Check `NwhWheelTelemetry` component has all 4 wheels assigned
- Verify Rigidbody has no constraints
- Try the Rigidbody fallback method (Method 3)

### Issue 2: Car Moves but Not Responsive to Agent
**Possible causes:**
- Behavior Parameters not set correctly
- Decision Requester missing or misconfigured
- Training not connected

**Solution:**
- Verify `Behavior Name` matches config file (`CarAgentParams`)
- Add `Decision Requester` component
- Check training script is running and connected

### Issue 3: Actions Received but Car Doesn't Respond
**Possible causes:**
- Wrong property names in reflection
- Properties are read-only
- Vehicle controller API changed

**Solution:**
- Check NWH WheelController3D documentation for correct property names
- Try Method 1 (NWH VP2 controller) if available
- Use Method 3 (Rigidbody fallback) as temporary solution

### Issue 4: Car Moves Randomly (Not Learning)
**Possible causes:**
- Agent is learning but poorly (normal in early training)
- Reward system not working
- Observations not correct

**Solution:**
- This is normal! Early training shows random behavior
- Check reward values in TensorBoard
- Verify observations are being collected correctly

## Testing the Fix

1. **Before Training:**
   - Test with Heuristic (manual control) - should work
   - Verify all components assigned

2. **During Training:**
   - Watch Unity - car should move (even if randomly at first)
   - Check Console for errors
   - Monitor TensorBoard for reward values

3. **Expected Behavior:**
   - **Early training (0-10k steps)**: Random movements, frequent crashes
   - **Mid training (10k-100k steps)**: Starts following track, fewer crashes
   - **Late training (100k+ steps)**: Smooth driving, better lap times

## Manual Vehicle Control Implementation

If the automatic detection doesn't work, you can manually implement vehicle control. Here's a template:

```csharp
void ApplyVehicleInputs(float steer, float throttle, float brake)
{
    // Replace with your actual vehicle controller code
    // Example for custom controller:
    var controller = GetComponent<YourVehicleController>();
    if (controller != null)
    {
        controller.SetSteering(steer);
        controller.SetThrottle(throttle);
        controller.SetBrake(brake);
    }
}
```

## Next Steps

1. ✅ Code updated - actions are now applied
2. ⚠️ Test in Unity - verify vehicle responds
3. ⚠️ Start training - watch for movement
4. ⚠️ Monitor progress - check TensorBoard

If the car still doesn't move after these steps, check:
- Unity Console for specific errors
- Vehicle controller documentation
- NWH WheelController3D API reference

