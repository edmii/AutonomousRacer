# Checking Saved Models and Testing Guide

## ✅ Your Model Status

**Good news!** Your model was saved before the restart. Here's what was found:

### Latest Checkpoint
- **Steps trained**: 549,996 steps
- **File**: `CarAgentParams-549996.onnx` (saved on 11/20/2025 8:50:02 PM)
- **Location**: `MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams\`

### Available Model Files
- **Main model**: `results\car_v0\CarAgentParams.onnx` (latest exported model)
- **Checkpoints**: Multiple checkpoints saved every 50,000 steps
- **Training state**: `checkpoint.pt` (for resuming training)

## 📋 How to Check Your Saved Models

### Option 1: Quick Check (Batch File - No Execution Policy Issues)
```cmd
check_model_status.bat
```

### Option 2: Check via PowerShell
```powershell
# Run the PowerShell script (bypasses execution policy)
powershell -ExecutionPolicy Bypass -File check_model_status.ps1

# Or manually list models:
Get-ChildItem "MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams\*.onnx" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 5 Name, LastWriteTime
```

### Option 2: Check Training Status
```powershell
# View training status JSON
Get-Content "MLAgentsEnv\mlagents-env\Scripts\results\car_v0\run_logs\training_status.json" | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

### Option 3: Check File Explorer
Navigate to: `MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams\`

## 🧪 How to Test Your Model in Unity

### Step 1: Copy Model to Unity Project
1. Copy the `.onnx` file to your Unity project's `Assets` folder (or a subfolder like `Models`)
   - **Recommended file**: `MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams.onnx`
   - Or use a specific checkpoint: `CarAgentParams-549996.onnx`

### Step 2: Configure Unity for Inference
1. **Select your Car GameObject** in Unity
2. **Find the Behavior Parameters component** in the Inspector
3. **Set the following:**
   - **Behavior Type**: Change from `Default` to `Inference Only`
   - **Model**: Drag your `.onnx` file into the Model field
   - **Behavior Name**: Should be `CarAgentParams` (must match)
4. **Make sure Decision Requester is still attached** (it should be)

### Step 3: Test the Model
1. **Press Play** in Unity
2. The car should now use the trained model to drive autonomously
3. Watch the Unity Console for any errors
4. The car should attempt to drive around the track

### Step 4: Monitor Performance
- Watch how the car drives
- Check if it follows the track
- Note any crashes or off-track behavior
- The model was trained for 549,996 steps, so it should have learned some behavior

## 🔄 How to Resume Training

If you want to continue training from where it left off:

### Option 1: Use Resume Script (Recommended)
```powershell
.\train_model_resume.ps1
```

### Option 2: Manual Resume Command
```powershell
cd MLAgentsEnv\mlagents-env\Scripts
mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --resume
```

**Note**: 
- Training will continue from the last checkpoint (549,996 steps)
- Make sure Unity is running and in Play mode before starting
- The model will continue training up to the max_steps limit (5,000,000 steps)

## 📊 View Training Progress

To see training graphs and metrics:

```powershell
cd MLAgentsEnv\mlagents-env\Scripts
tensorboard --logdir=results
```

Then open your browser to: `http://localhost:6006`

## ⚠️ Troubleshooting

### Model Not Working in Unity
1. **Check Behavior Parameters:**
   - Behavior Type must be `Inference Only` (not `Default` or `Heuristic Only`)
   - Model file must be assigned
   - Behavior Name must match: `CarAgentParams`

2. **Check Unity Console:**
   - Look for errors about model loading
   - Make sure the `.onnx` file is in the Unity project (not just referenced)

3. **Verify Model Compatibility:**
   - The model was trained with 22 observations and 2 continuous actions
   - Make sure your CarAgent setup matches this

### Resume Training Not Working
1. **Check if checkpoint exists:**
   ```powershell
   Test-Path "MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams\checkpoint.pt"
   ```

2. **Make sure Unity is running:**
   - Unity must be in Play mode
   - Academy component must be in the scene
   - Car agent must be active

3. **Check for errors:**
   - Look at the training console output
   - Check Unity Console for connection errors

## 📝 Summary

✅ **Your model was saved!** Latest checkpoint: 549,996 steps  
✅ **To test**: Copy `.onnx` to Unity, set Behavior Type to `Inference Only`  
✅ **To resume**: Run `train_model_resume.ps1`  
✅ **To monitor**: Use TensorBoard with `tensorboard --logdir=results`

Good luck with your autonomous racer! 🏎️

