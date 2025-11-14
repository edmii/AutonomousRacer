# ML-Agents Training Setup for Autonomous Racer

## Overview
This project uses Unity ML-Agents to train a neural network for autonomous racing. The neural network learns to control a car using raycast sensors, wheel telemetry, and checkpoint progress.

## Neural Network Architecture

### Observations (Input Space)
The agent receives **~22 observations** per step:
- **13 raycast distances** (normalized 0-1): Obstacle detection in front and sides
- **1 speed** (normalized 0-1): Current velocity magnitude
- **1 yaw rate** (normalized -1 to 1): Angular velocity around Y-axis
- **2 slip values** (normalized 0-1): Lateral and longitudinal wheel slip
- **3 wheel states** (binary): Locked, spinning, on-track
- **2 torque values** (normalized 0-1): Motor and brake torque

### Actions (Output Space)
The agent outputs **2 continuous actions**:
- **Steering** (-1 to 1): Left/right steering input
- **Acceleration/Brake** (-1 to 1): Negative = brake, Positive = throttle

### Network Architecture
- **Type**: PPO (Proximal Policy Optimization)
- **Layers**: 3 hidden layers
- **Hidden Units**: 512 per layer
- **Normalization**: Enabled (recommended for mixed observation types)

## Reward System

The agent receives rewards/penalties for:

1. **Speed Reward** (`speedRewardMultiplier = 0.01`)
   - Rewards forward movement proportional to speed
   - Only applies when speed > `minSpeedForProgress` (0.5 m/s)

2. **Forward Progress** (`forwardProgressMultiplier = 0.02`)
   - Additional reward for moving forward (not backward)
   - Encourages forward direction

3. **On-Track Reward** (`onTrackReward = 0.01`)
   - Small reward per step when all wheels are on track
   - Encourages staying on the racing surface

4. **Checkpoint Rewards**
   - **+0.2**: Correct checkpoint hit
   - **+1.0**: Bonus for completing a lap
   - **-0.3**: Wrong checkpoint (going backwards or skipping)

5. **Slip Penalty** (`slipPenaltyMultiplier = 0.1`)
   - Penalizes excessive wheel slipping
   - Helps learn smooth driving

6. **Wheel Event Penalties** (`wheelEventPenalty = 0.05`)
   - **-0.05**: Wheel locked
   - **-0.025**: Wheel spinning
   - Encourages proper traction control

7. **Obstacle Avoidance** (`obstaclePenaltyMultiplier = 0.5`)
   - Penalizes getting too close to obstacles (walls, barriers)
   - Threshold: `obstacleThreshold = 0.2` (normalized distance)

8. **Episode Termination Penalties**
   - **-1.0**: Crash (obstacle too close)
   

## Training Configuration

### Hyperparameters (Car_ppo.yaml)
- **Learning Rate**: 3.0e-4
- **Batch Size**: 1024
- **Buffer Size**: 40960
- **Time Horizon**: 512 steps
- **Gamma (Discount)**: 0.99
- **Max Steps**: 5,000,000
- **Epsilon (PPO clip)**: 0.2
- **Lambda (GAE)**: 0.95

### Episode Termination
Episodes end when:
- Car crashes (raycast distance < `crashThreshold = 0.05`)
- Car goes off-track (`endOnOffTrack = true`)
- Maximum steps reached
- Manual termination

## How to Train

### Prerequisites
1. Unity environment running with the car agent
2. ML-Agents Python package installed (in `MLAgentsEnv\mlagents-env\`)
3. All components properly assigned in Unity Inspector:
   - `CarAgent` component
   - `RaycastSensor` component
   - `NwhWheelTelemetry` component
   - `CheckpointManager` and checkpoints

### CUDA/GPU Setup (Recommended)
ML-Agents **automatically detects and uses CUDA** if available. GPU acceleration significantly speeds up training (often 10-100x faster than CPU).

**To check CUDA availability:**
```bash
python check_cuda.py
```

**If CUDA is not available:**
1. Install NVIDIA GPU drivers (latest version)
2. Install CUDA toolkit (version 12.1 recommended)
3. Reinstall PyTorch with CUDA support:
   ```bash
   pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
   ```

**Note:** The training script (`train_model.bat`) automatically checks CUDA availability before starting. ML-Agents will use GPU if available, otherwise fall back to CPU (slower but functional).

### Training Steps

1. **Start Unity Environment**
   - Open your Unity scene
   - Make sure the car has all required components
   - Press Play to start the environment

2. **Start Training**
   
   **Starting a NEW Training Session:**
   
   **In PowerShell (recommended):**
   ```powershell
   # Option 1: New training (single environment) - RECOMMENDED
   .\train_model.ps1
   
   # Option 2: New training (batch script)
   .\train_model.bat
   ```
   
   **In Command Prompt:**
   ```cmd
   train_model.bat
   ```
   
   **Resuming Previous Training:**
   
   **In PowerShell:**
   ```powershell
   # Resume from last checkpoint
   .\train_model_resume.ps1
   ```
   
   **In Command Prompt:**
   ```cmd
   train_model_resume.bat
   ```
   
   **Parallel Training (faster, requires Unity build):**
   ```powershell
   .\train_model_parallel.ps1
   ```
   
   **Manual Commands:**
   ```bash
   cd MLAgentsEnv\mlagents-env\Scripts
   
   # New training
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force
   
   # Resume training
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --resume
   
   # Parallel training (4 environments)
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-envs=4 --env-path=path\to\build.exe
   ```
   
   **Note:** 
   - **New Training**: Starts from scratch with random weights
   - **Resume Training**: Continues from last checkpoint (saved every 50,000 steps)
   - See `TRAINING_SESSIONS_GUIDE.md` for detailed information
   - For parallel training, see `PARALLEL_TRAINING_GUIDE.md`

3. **Monitor Training**
   - TensorBoard: `tensorboard --logdir=results`
   - Check Unity Console for episode logs
   - Watch reward values increase over time

4. **Stop Training**
   
   **Graceful Stop (Recommended):**
   - Press `Ctrl+C` in the training terminal/PowerShell window
   - ML-Agents will:
     - Finish current training step
     - Save the current model
     - Generate training graphs
     - Clean up connections
   - Wait for the "Learning was interrupted" message
   - Your progress is saved!
   
   **Force Stop (If Ctrl+C doesn't work):**
   - Close the terminal/PowerShell window
   - Stop Unity (if using Editor, press Stop)
   - Last checkpoint will be available (saved every 50,000 steps)
   
   **Note:** 
   - Models are saved automatically during training
   - Checkpoints are saved every 50,000 steps
   - Interrupting with Ctrl+C is safe - your progress is preserved

5. **Checkpoints & Resuming**
   - **Checkpoints saved**: Every 50,000 steps automatically
   - **Location**: `results\car_v0\CarAgentParams\`
   - **Keep last**: 5 checkpoints (configurable)
   - **On stop**: Checkpoint saved when you press Ctrl+C gracefully
   - **To resume**: Use `train_model_resume.ps1` or `--resume` flag
   - **See**: `TRAINING_SESSIONS_GUIDE.md` for detailed instructions

## Tuning Tips

### If agent is too slow:
- Increase `speedRewardMultiplier`
- Increase `forwardProgressMultiplier`
- Decrease `minSpeedForProgress`

### If agent crashes too much:
- Increase `obstaclePenaltyMultiplier`
- Decrease `crashThreshold`
- Increase raycast ranges in `RaycastSensor`

### If agent goes off-track:
- Increase `onTrackReward`
- Decrease `endOnOffTrack` threshold (or add grace period)
- Check `trackSurfaceMask` in `WheelTelemetry`

### If training is unstable:
- Decrease `learning_rate` (try 1.0e-4)
- Increase `batch_size`
- Increase `time_horizon`

## Model Files

After training, the model will be saved as:
- **ONNX**: `CarAgentParams.onnx` (for inference)
- **PyTorch**: `CarAgentParams.pt` (for continued training)
- **Checkpoint**: `checkpoint.pt` (training state)

To use the trained model in Unity:
1. Copy the `.onnx` file to your Unity project
2. Assign it to the `Behavior Parameters` component
3. Set `Behavior Type` to `Inference Only`

## Next Steps

1. **Start Training**: Run `train_model.bat` or the mlagents-learn command
2. **Monitor Progress**: Watch TensorBoard and Unity console
3. **Adjust Rewards**: Tune reward multipliers based on behavior
4. **Iterate**: Let it train for several hours/days for best results

Good luck with your autonomous racer!

