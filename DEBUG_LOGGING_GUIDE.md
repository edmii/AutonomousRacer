# Debug Logging Guide - Agent Input System

This guide explains the debug logging added to help diagnose why the car isn't moving during training.

## Overview

Comprehensive logging has been added to `CarAgent.cs` to track:
1. **Actions received from the agent** - Raw and processed values
2. **Input method used** - Which vehicle control method is being applied
3. **Environment status** - Connection state, episode progress, speed
4. **Unity-MLAgents connection** - Verification that training is connected

## Logging Configuration

In Unity Inspector, on the `CarAgent` component:
- **Enable Debug Logging**: `true` (default) - Turn on/off all debug logs
- **Log Frequency**: `50` (default) - Log every N frames (0 = every frame, 50 = every 50 frames)

## What to Look For in Unity Console

### 1. Initialization Logs

When the agent starts, you should see:
```
[CarAgent] Initialize - Agent initialized. Rigidbody: OK, WheelTelemetry: OK, RaySensor: OK
```

**If you see MISSING:**
- Check that all required components are assigned in the Inspector
- Verify the car GameObject has Rigidbody, NwhWheelTelemetry, and RaycastSensor components

### 2. Episode Start Logs

At the beginning of each episode:
```
[CarAgent] OnEpisodeBegin - Episode started. Position: (x, y, z), Velocity reset. Action count reset to 0.
```

**What this tells you:**
- Episodes are starting correctly
- The agent is resetting between episodes

### 3. Action Logs (Most Important!)

Every N frames (based on Log Frequency), you'll see:
```
[CarAgent] Action #50 - Raw: [Steer=0.123, Accel=0.456] | Processed: [Steer=0.123, Throttle=0.456, Brake=0.000] | Method: NWH WheelController3D (Steer:True, Motor:True, Brake:True)
```

**What to check:**
- **Raw values**: Should be non-zero if the model is running. If always 0.000, the model isn't sending actions.
- **Processed values**: Should match the raw values (after clamping/scaling)
- **Method**: Shows which input method is being used

**If Raw values are always 0.000:**
- The model isn't connected or isn't generating actions
- Check that training script is running
- Verify Behavior Parameters > Behavior Type is set to "Default" (not "Heuristic Only")
- Check for "Connected new brain: CarAgentParams" message in Unity Console

**If Method shows "None" or "WARNING: No input method applied":**
- Vehicle controller setup is incorrect
- Check that wheel controllers are assigned in NwhWheelTelemetry
- Verify NWH WheelController3D properties are accessible

### 4. Input Application Logs

When actions are applied, you'll see one of these:

**NWH Vehicle Controller:**
```
[CarAgent] Applied via NWH Vehicle Controller - Steer: 0.123, Throttle: 0.456, Brake: 0.000
```

**NWH WheelController3D:**
```
[CarAgent] Applied via NWH WheelController3D - SteerAngle: 3.69°, MotorTorque: 456.0Nm, BrakeTorque: 0.0Nm
```

**Rigidbody Fallback:**
```
[CarAgent] Applied via Rigidbody Fallback - Steer: 0.123, Throttle: 0.456, Brake: 0.000
```

**Warning (No input applied):**
```
[CarAgent] WARNING: No input method applied! Check vehicle controller setup. Steer: 0.123, Throttle: 0.456, Brake: 0.000
```

**What this tells you:**
- Which method is being used to control the car
- The actual values being applied (torques, angles, forces)
- If no method is working, you'll see the warning

### 5. Status Logs (Every 5 seconds)

Periodic status updates:
```
[CarAgent] Status - Frame: 300, Episode: 12.34, Speed: 5.67 m/s, Actions received: 300, Input method: NWH WheelController3D (Steer:True, Motor:True, Brake:True)
```

**What to check:**
- **Speed**: Should be > 0 if the car is moving. If always 0.00, the car isn't moving despite receiving actions.
- **Actions received**: Should increase over time. If it stays at 0, no actions are being received.
- **Input method**: Confirms which control method is active

## Troubleshooting Checklist

### Car Doesn't Move at All

1. **Check Unity Console for action logs:**
   - If you see `[CarAgent] Action #X` logs → Actions are being received ✓
   - If you DON'T see action logs → Model isn't connected or not sending actions

2. **Check the input method:**
   - If you see "WARNING: No input method applied" → Vehicle controller setup issue
   - If you see "Applied via..." → Input is being sent, but car still doesn't move → Physics issue

3. **Check speed in status logs:**
   - If speed is always 0.00 → Car isn't moving despite receiving inputs
   - Possible causes: Rigidbody constraints, wheel setup, physics settings

### Model Not Sending Actions

1. **Check Unity Console for connection:**
   - Look for: "Connected new brain: CarAgentParams"
   - If missing → Training script isn't connected

2. **Check Behavior Parameters:**
   - Behavior Type should be "Default" (not "Heuristic Only" or "Inference Only")
   - Behavior Name should match config file: "CarAgentParams"

3. **Check training script:**
   - Is `train_model.bat` running?
   - Are there any errors in the training console?
   - Try running `verify_unity_connection.py` to test connection

### Actions Received but Car Doesn't Respond

1. **Check which input method is being used:**
   - If "NWH WheelController3D" → Verify wheels are assigned in NwhWheelTelemetry
   - If "Rigidbody Fallback" → This should work, but may be less realistic
   - If "WARNING" → No method is working, check component setup

2. **Check the applied values:**
   - MotorTorque should be > 0 when throttle > 0
   - SteerAngle should change when steering input changes
   - If values are always 0 → Input processing issue

## Testing the Connection

Run the verification script to test Unity connection:
```bash
cd MLAgentsEnv\mlagents-env\Scripts
python ..\..\..\verify_unity_connection.py
```

This will:
- Connect to Unity
- Send a test action
- Verify the connection is working
- Show you what to look for in Unity Console

## Expected Log Flow During Training

1. **Startup:**
   ```
   [CarAgent] Initialize - Agent initialized...
   ```

2. **Episode Start:**
   ```
   [CarAgent] OnEpisodeBegin - Episode started...
   ```

3. **During Training (every 50 frames):**
   ```
   [CarAgent] Action #50 - Raw: [Steer=0.123, Accel=0.456] | Processed: [Steer=0.123, Throttle=0.456, Brake=0.000] | Method: NWH WheelController3D
   [CarAgent] Applied via NWH WheelController3D - SteerAngle: 3.69°, MotorTorque: 456.0Nm, BrakeTorque: 0.0Nm
   ```

4. **Periodic Status (every 5 seconds):**
   ```
   [CarAgent] Status - Frame: 300, Episode: 12.34, Speed: 5.67 m/s, Actions received: 300, Input method: NWH WheelController3D
   ```

## Disabling Logs

To reduce console spam during normal training:
1. Set `Enable Debug Logging` to `false` in Unity Inspector
2. Or increase `Log Frequency` to a higher value (e.g., 300 = every 5 seconds)

## Next Steps

If you see actions being received but the car still doesn't move:
1. Check the "Applied via..." logs to see which method is used
2. Verify the input method's requirements (wheels assigned, properties accessible)
3. Check Unity's Physics settings and Rigidbody constraints
4. Try the Rigidbody Fallback method if NWH methods aren't working

