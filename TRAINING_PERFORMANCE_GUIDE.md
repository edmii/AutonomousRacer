# Training Performance Guide

## 📊 Quick Stats Analysis (Based on your data)

Your stats show exactly where the problem is:

- **Render Thread: 52.6ms**: This is huge! It means **~94% of your frame time** is spent just drawing the scene (Shadows, Batches).
- **CPU Main: 56.1ms**: The physics and logic are waiting for the rendering.
- **Speed Achieved**: With `Max Allowed Timestep = 0.35`, you are hitting **~6.2x speed**.

**The Good News**: You broke the 1x speed barrier!
**The Better News**: If you disable graphics, you could easily hit **20x speed**.

---

## 🚨 The Physics Bottleneck Explained

If you are getting **20 FPS** (50ms) during training with `time_scale = 20`, **your CPU is the bottleneck**.

### The Math of Why You Are Capped

Your current settings create a "Speed Trap":

1. **FPS**: 20 frames/sec = **0.05 seconds** per frame (Real Time)
2. **Target Speed**: 20x (Requesting 1.0 second of game time per frame)
3. **Safety Limit**: `Maximum Allowed Timestep` = **0.05 seconds** (Default)

**The Result**: 
Unity tries to advance time by 1.0 second, but the "Safety Limit" caps it at 0.05 seconds.
- Real Time passed: 0.05s
- Game Time passed: 0.05s
- **Actual Speed**: 1x (Normal Speed)

**With Max Timestep = 0.35** (Your new setting):
- Real Time: 0.056s
- Game Time: 0.35s (capped)
- **Actual Speed**: 0.35 / 0.056 = **~6.2x Speed**

## 🧪 How to Verify

### Method 1: Use the Monitor Script (Recommended)
1. Attach the `Scripts/TrainingPerformanceMonitor.cs` to your Car.
2. Watch the Console logs.
3. You will see something like: `[Performance] ✅ OK | Speed: 6.2x (Target: 20x) | FPS: 18`

## 🚀 How to Speed Up Training

You are **Render Bound**. You need to stop drawing things to go faster.

### Option 1: Disable Graphics in Editor (Easy)
1. **Disable Shadows**: Edit > Project Settings > Quality > Shadows > Disable.
   - *Expected Gain*: +20-30 FPS.
2. **Hide Game View**: Minimize the Game window or uncheck "Maximize on Play".
3. **Disable Stats**: The Stats window itself has overhead!

### Option 2: Build with No Graphics (Biggest Gain)
1. **Build the game** (File → Build Settings → Build).
2. Run training on the **Executable** instead of Editor:
   ```bash
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --resume --env-path=Builds\AutonomousRacer.exe --no-graphics
   ```
   - *Expected Gain*: **Hit the full 20x speed limit**.

### Option 3: Parallel Training (Best Solution)
Use multiple environments to utilize all CPU cores.
1. See `PARALLEL_TRAINING_GUIDE.md`.
2. Running 4 instances at 6x speed = 24x total training speed.

## Summary
- **Current Status**: Training at ~6.2x speed (Render limited).
- **Bottleneck**: Rendering (Shadows/Batches) is taking 52ms per frame.
- **Immediate Fix**: **Disable Shadows** in Quality Settings or use `--no-graphics` build.
