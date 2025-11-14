# Parallel Training Guide for ML-Agents

## Overview

ML-Agents supports **parallel training** to significantly speed up learning by running multiple environments simultaneously. This can provide **2-10x speedup** depending on your hardware.

## Two Types of Parallel Training

### 1. Multiple Unity Environments (`--num-envs`)
- Runs **multiple Unity instances** in parallel
- Each instance collects experiences independently
- **Requires Unity BUILD** (executable), not Editor
- Best for: Maximum speedup, dedicated training machines

### 2. Multiple Training Areas (`--num-areas`)
- Multiple agents in **same Unity scene**
- Each area has its own agent instance
- Can work with Unity Editor (if scene supports it)
- Best for: Quick testing, single Unity instance

## Current Configuration

Your current setup:
- **Single environment** (`--num-envs=1` default)
- **Single area** (`--num-areas=1` default)
- **Threaded: false** (internal threading disabled)

## How to Enable Parallel Training

### Option 1: Multiple Environments (Recommended for Speed)

**Requirements:**
1. Build your Unity project as an executable
2. Use the build path instead of Editor

**Steps:**

1. **Build Unity Project:**
   - File → Build Settings
   - Choose platform (Windows, Linux, etc.)
   - Click "Build"
   - Save as `Builds/AutonomousRacer.exe` (or similar)

2. **Update Training Script:**
   ```bash
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force ^
       --num-envs=4 ^
       --env-path=..\..\..\Builds\AutonomousRacer.exe
   ```

3. **Benefits:**
   - 4x more experiences collected simultaneously
   - Faster training (if CPU/GPU can handle it)
   - No Unity Editor needed

**Example with 4 environments:**
```bash
cd MLAgentsEnv\mlagents-env\Scripts
mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-envs=4 --env-path=..\..\..\Builds\AutonomousRacer.exe
```

### Option 2: Multiple Training Areas (Works with Editor)

**Requirements:**
1. Modify your Unity scene to have multiple training areas
2. Each area needs its own agent spawn point
3. Can use Unity Editor

**Steps:**

1. **Modify Unity Scene:**
   - Duplicate your track/agent setup
   - Create 4 separate training areas
   - Each area should be isolated (no collisions between areas)

2. **Update Training Command:**
   ```bash
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-areas=4
   ```

3. **Benefits:**
   - Works with Unity Editor
   - No build required
   - Multiple agents learning simultaneously

**Note:** This requires scene modifications to support multiple isolated training areas.

### Option 3: Combined (Maximum Parallelism)

Use both for maximum speed:
```bash
mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force ^
    --num-envs=2 ^
    --num-areas=2 ^
    --env-path=..\..\..\Builds\AutonomousRacer.exe
```

This gives you **4 total parallel agents** (2 environments × 2 areas each).

## Performance Considerations

### CPU Usage
- **Single environment**: ~1-2 CPU cores
- **4 environments**: ~4-8 CPU cores
- **8 environments**: ~8-16 CPU cores

### Memory Usage
- Each Unity instance uses ~500MB-2GB RAM
- 4 environments = ~2-8GB RAM
- Monitor with Task Manager

### GPU Usage
- Training computation scales with number of environments
- CUDA automatically handles parallel training
- More environments = better GPU utilization

### Recommended Settings

**For 4-core CPU:**
```bash
--num-envs=2 --num-areas=1
```

**For 8-core CPU:**
```bash
--num-envs=4 --num-areas=1
```

**For 16+ core CPU:**
```bash
--num-envs=8 --num-areas=1
```

**For GPU training:**
- Can handle more environments
- Try `--num-envs=4` to start
- Increase if GPU utilization is low

## Updated Training Scripts

### Single Environment (Current)
```bash
train_model.bat
```

### Parallel Environments
```bash
train_model_parallel.bat
```

**Note:** Update the `--env-path` in `train_model_parallel.bat` to point to your Unity build.

## Configuration File Changes

You can also enable internal threading in `Car_ppo.yaml`:

```yaml
threaded: true  # Enable internal threading (default: false)
```

**Note:** This is different from parallel environments - it's for internal optimization.

## Troubleshooting

### "num_envs must be 1 if env_path is not set"
- **Solution:** Either:
  - Set `--env-path` to Unity build executable
  - Or use `--num-areas` instead of `--num-envs`

### Out of Memory
- **Solution:** Reduce `--num-envs` (try 2 instead of 4)
- Or reduce `batch_size` in config

### Unity Editor Crashes with num-envs > 1
- **Solution:** Use Unity build instead of Editor
- Or use `--num-areas` instead

### Slow Training with Multiple Environments
- **Solution:** Your CPU/GPU may be bottlenecked
- Reduce `--num-envs` to 2
- Check CPU/GPU utilization

## Expected Speedup

| Configuration | Speedup | Use Case |
|---------------|---------|----------|
| 1 env, 1 area | 1x (baseline) | Development, debugging |
| 2 envs, 1 area | ~1.8x | Good balance |
| 4 envs, 1 area | ~3.2x | Recommended for most |
| 8 envs, 1 area | ~5.0x | High-end systems |
| 4 envs, 2 areas | ~5.5x | Maximum (if scene supports) |

**Note:** Speedup depends on CPU/GPU power. Diminishing returns after 4-8 environments.

## Quick Start

1. **For fastest training (requires build):**
   ```bash
   # Build Unity project first, then:
   train_model_parallel.bat
   ```

2. **For Editor-based training (requires scene mods):**
   ```bash
   mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-areas=4
   ```

3. **Monitor performance:**
   - Watch CPU/GPU usage
   - Check training speed in TensorBoard
   - Adjust `--num-envs` based on utilization

## Summary

✅ **Parallel training is supported** - use `--num-envs` or `--num-areas`  
✅ **Multiple environments** require Unity builds (not Editor)  
✅ **Multiple areas** can work with Editor (if scene supports it)  
✅ **Recommended**: Start with `--num-envs=4` for good speedup  
✅ **Monitor resources** - don't overload your system

Start with 2-4 parallel environments and adjust based on your hardware performance!

