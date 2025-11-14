# Unity ML-Agents Connection Setup Guide

This guide explains how to set up Unity to connect with the Python training script.

## Prerequisites in Unity

Before running the verification script or training, make sure:

### 1. ML-Agents Academy in Scene
- Your scene must have an **ML-Agents Academy** component
- This is usually on a GameObject called "Academy" or "ML-Agents Academy"
- If missing, create an empty GameObject and add the "Academy" component from ML-Agents

### 2. Car Agent Setup
Your car GameObject must have:
- ✅ **CarAgent** component (your custom script)
- ✅ **Behavior Parameters** component
  - Behavior Name: `CarAgentParams` (must match config file)
  - Behavior Type: `Default` (NOT "Heuristic Only" or "Inference Only")
  - Vector Observation Space Size: `22` (or your observation count)
  - Continuous Actions: `2`
- ✅ **Decision Requester** component
  - Decision Period: `1`
  - Take Actions Between Decisions: `true`

### 3. Required Components on Car
- **Rigidbody** component
- **NwhWheelTelemetry** component (with wheels assigned)
- **RaycastSensor** component
- **CarAgent** component (with all references assigned)

## Connection Process

### Step 1: Start Unity
1. Open your Unity project
2. Open the scene with your car and track
3. **DO NOT press Play yet**

### Step 2: Verify Setup
1. Select your car GameObject
2. Check Inspector for all required components
3. Verify Behavior Parameters settings

### Step 3: Press Play
1. Press **Play** button in Unity
2. Watch the Unity Console
3. You should see: **"Waiting for connection..."** or **"Connected new brain: CarAgentParams"**

### Step 4: Run Python Script
1. Open terminal/command prompt
2. Navigate to project root
3. Run: `cd MLAgentsEnv\mlagents-env\Scripts && python ..\..\..\verify_unity_connection.py`
4. Or run training: `train_model.bat`

## What You Should See

### In Unity Console (when Play is pressed):
```
[Academy] Waiting for connection...
```
or
```
[Academy] Connected new brain: CarAgentParams
```

### In Python Script:
```
✓ Successfully connected to Unity environment!
✓ Behavior specs: ['CarAgentParams']
```

## Common Issues

### Issue: "Timeout" or "took too long to respond"
**Causes:**
- Unity is not in Play mode
- Unity is paused
- ML-Agents Academy missing from scene
- Behavior Parameters not set correctly

**Solution:**
1. Stop Play mode in Unity
2. Verify Academy component exists
3. Check Behavior Parameters settings
4. Press Play again
5. Wait for "Waiting for connection..." message
6. Then run Python script

### Issue: "No agents found"
**Causes:**
- Car GameObject is inactive
- Car is not in the scene
- Behavior Parameters not set up

**Solution:**
1. Make sure car GameObject is active (checkbox checked in Hierarchy)
2. Verify car is in the scene (not prefab)
3. Check Behavior Parameters component

### Issue: "Port already in use"
**Causes:**
- Another Unity instance is running
- Previous connection didn't close properly
- Another Python script is connected

**Solution:**
1. Close all Unity instances
2. Close any Python scripts
3. Restart Unity
4. Try again

### Issue: Unity Console shows errors
**Check for:**
- Missing component references in CarAgent
- Null reference exceptions
- Behavior Parameters configuration errors

**Solution:**
- Fix all errors in Unity Console first
- Make sure all component references are assigned in Inspector

## Verification Checklist

Before running training, verify:

- [ ] Unity scene has ML-Agents Academy component
- [ ] Car GameObject is active in scene
- [ ] CarAgent component has all references assigned
- [ ] Behavior Parameters component exists and is configured
- [ ] Behavior Type is set to "Default"
- [ ] Decision Requester component is added
- [ ] Unity is in Play mode
- [ ] Unity Console shows "Waiting for connection..." or "Connected"
- [ ] No errors in Unity Console

## Testing the Connection

1. **Run verification script:**
   ```bash
   cd MLAgentsEnv\mlagents-env\Scripts
   python ..\..\..\verify_unity_connection.py
   ```

2. **If successful, you should see:**
   - Connection established message
   - Behavior specs listed
   - Test action sent
   - Unity Console shows `[CarAgent] Action #X` logs

3. **If it fails:**
   - Check Unity Console for errors
   - Verify all checklist items above
   - Make sure Unity is in Play mode
   - Try stopping and restarting Play mode

## Next Steps

Once connection is verified:
- You can run `train_model.bat` to start training
- Watch Unity Console for action logs
- Monitor training progress in Python console

