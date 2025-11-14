# Training Sessions Guide: Starting New vs Resuming

## Overview

ML-Agents supports two types of training sessions:
1. **New Training Session** - Starts from scratch with random weights
2. **Resume Training** - Continues from the last checkpoint

## Starting a New Training Session

### When to Use
- First time training
- Want to start completely fresh
- Previous training didn't work well
- Changed reward system or observations significantly

### How to Start New Training

**PowerShell:**
```powershell
.\train_model.ps1
```

**Command Prompt:**
```cmd
train_model.bat
```

**Manual Command:**
```bash
cd MLAgentsEnv\mlagents-env\Scripts
mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force
```

**What Happens:**
- Creates new directory: `results\car_v0\`
- Starts with random neural network weights
- `--force` flag overwrites if `car_v0` already exists
- Training starts from step 0

### Using a Different Run ID (New Session)

To start a new session without overwriting previous training:

```powershell
cd MLAgentsEnv\mlagents-env\Scripts
mlagents-learn Config\Car_ppo.yaml --run-id=car_v1 --force
```

This creates a separate training run (`car_v1`) without affecting `car_v0`.

---

## Resuming Previous Training

### When to Use
- Training was interrupted (Ctrl+C, crash, etc.)
- Want to continue training the same model
- Reached max_steps but want to train more
- Computer restarted during training

### How to Resume Training

**PowerShell:**
```powershell
.\train_model_resume.ps1
```

**Command Prompt:**
```cmd
train_model_resume.bat
```

**Manual Command:**
```bash
cd MLAgentsEnv\mlagents-env\Scripts
mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --resume
```

**What Happens:**
- Loads the last checkpoint automatically
- Continues from the last saved step
- Preserves all training history
- TensorBoard graphs continue seamlessly

### Prerequisites for Resuming
- Previous training must exist at `results\car_v0\`
- Checkpoint files must be present
- Same `run-id` as previous training
- Same behavior name (`CarAgentParams`)

---

## Understanding Checkpoints

### Checkpoint Location
```
MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams\
```

### Checkpoint Files
- `checkpoint.pt` - Latest checkpoint (training state)
- `CarAgentParams-50000.pt` - Checkpoint at 50,000 steps
- `CarAgentParams-100000.pt` - Checkpoint at 100,000 steps
- `CarAgentParams.onnx` - Latest ONNX model (for inference)

### Checkpoint Frequency
- **Automatic**: Every 50,000 steps (configurable in `Car_ppo.yaml`)
- **On Stop**: When you press Ctrl+C gracefully
- **On Crash**: Last checkpoint before crash

### Checkpoint Settings (Car_ppo.yaml)
```yaml
checkpoint_interval: 50000  # Save checkpoint every N steps
keep_checkpoints: 5         # Keep last 5 checkpoints
```

---

## Comparison Table

| Feature | New Training | Resume Training |
|---------|-------------|----------------|
| **Command** | `--force` | `--resume` |
| **Starting Point** | Step 0 | Last checkpoint |
| **Weights** | Random | Previous weights |
| **History** | New | Continues |
| **TensorBoard** | New graphs | Continues graphs |
| **Use Case** | Fresh start | Continue training |

---

## Step-by-Step Examples

### Example 1: First Time Training
```powershell
# 1. Start Unity and press Play
# 2. Run new training
.\train_model.ps1
```

### Example 2: Training Interrupted
```powershell
# Training was running, you pressed Ctrl+C
# Now resume:
.\train_model_resume.ps1
```

### Example 3: Training Completed, Want More
```powershell
# Training reached max_steps (5M steps)
# Increase max_steps in Car_ppo.yaml, then:
.\train_model_resume.ps1
```

### Example 4: Multiple Training Runs
```powershell
# Run 1: Initial training
.\train_model.ps1  # Creates car_v0

# Run 2: Different hyperparameters (new run)
cd MLAgentsEnv\mlagents-env\Scripts
mlagents-learn Config\Car_ppo.yaml --run-id=car_v1 --force

# Run 3: Resume original training
.\train_model_resume.ps1  # Resumes car_v0
```

---

## Troubleshooting

### Issue: "No previous training found"
**Cause:** No checkpoint exists at `results\car_v0\`

**Solution:**
- Use `train_model.ps1` to start new training
- Or check if you used a different `run-id`

### Issue: "Cannot resume - checkpoint not found"
**Cause:** Checkpoint files were deleted or corrupted

**Solution:**
- Check `results\car_v0\CarAgentParams\` for checkpoint files
- If missing, start new training with `--force`

### Issue: "Behavior name mismatch"
**Cause:** Changed behavior name in config or Unity

**Solution:**
- Use same behavior name as previous training
- Or start new training with different `run-id`

### Issue: "Resume starts from step 0"
**Cause:** Checkpoint loading failed silently

**Solution:**
- Check Unity Console for errors
- Verify checkpoint files exist
- Try manual resume command with verbose output

---

## Best Practices

1. **Use Descriptive Run IDs**
   - `car_v0` - Initial training
   - `car_v1_tuned_rewards` - Tuned reward system
   - `car_v2_faster` - Different hyperparameters

2. **Keep Checkpoints**
   - Don't delete `results\` folder
   - Keep at least last 5 checkpoints (default setting)

3. **Monitor Training**
   - Use TensorBoard to track progress
   - Check checkpoint files are being created

4. **Backup Important Models**
   - Copy `.onnx` files to safe location
   - Save best performing checkpoints

5. **Document Your Runs**
   - Note what changed between runs
   - Track which run performed best

---

## Quick Reference

### New Training
```powershell
.\train_model.ps1
# or
mlagents-learn Config\Car_ppo.yaml --run-id=NEW_ID --force
```

### Resume Training
```powershell
.\train_model_resume.ps1
# or
mlagents-learn Config\Car_ppo.yaml --run-id=EXISTING_ID --resume
```

### Check Checkpoints
```powershell
# List available checkpoints
dir MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams\*.pt
```

### View Training Progress
```powershell
# Start TensorBoard
cd MLAgentsEnv\mlagents-env\Scripts
tensorboard --logdir=results
# Then open http://localhost:6006 in browser
```

---

## Summary

- **New Training**: Use `train_model.ps1` or `--force` flag
- **Resume Training**: Use `train_model_resume.ps1` or `--resume` flag
- **Checkpoints**: Saved every 50,000 steps automatically
- **Safe to Interrupt**: Ctrl+C saves checkpoint before stopping
- **Multiple Runs**: Use different `run-id` values

Choose the right method based on whether you want to start fresh or continue previous training!

