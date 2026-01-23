# Reward Formula Documentation & Scenario Simulations

## Complete Reward Formula

The agent's reward is calculated **per step** (each FixedUpdate call) using the following components:

### Per-Step Rewards (Calculated in `CalculateRewards()`)

#### 1. Speed Reward
```
IF forwardSpeed > minSpeedForProgress (0.5 m/s):
    reward += forwardSpeed × speedRewardMultiplier (0.01)
```
- **Purpose**: Encourage forward movement
- **Range**: 0 to ~0.8 (at 80 m/s max speed)
- **Example**: At 20 m/s → +0.20 reward

#### 2. Forward Progress Reward
```
IF forwardSpeed > 0:
    reward += forwardSpeed × forwardProgressMultiplier (0.02)
```
- **Purpose**: Additional incentive for forward direction (not backward)
- **Range**: 0 to ~1.6 (at 80 m/s max speed)
- **Example**: At 20 m/s → +0.40 reward

#### 3. On-Track Reward
```
IF wheelsOnTrack:
    reward += onTrackReward (0.01)
ELSE:
    reward += -1.0  // Large penalty for going off track
```
- **Purpose**: Keep car on racing surface
- **Range**: +0.01 (on track) or -1.0 (off track)
- **Note**: Off-track penalty is 100x larger than on-track reward

#### 4. Slip Penalty (Always Applied)
```
totalSlip = |maxAbsLatSlip| + |maxAbsLongSlip|
reward -= totalSlip × slipPenaltyMultiplier (0.1)
```
- **Purpose**: Discourage excessive wheel slipping
- **Range**: 0 to -∞ (proportional to slip)
- **Example**: totalSlip = 0.5 → -0.05 penalty

#### 5. Traction Reward (Conditional)
```
IF forwardSpeed > minSpeedForProgress (0.5 m/s) AND totalSlip < goodTractionSlipThreshold (0.3):
    tractionQuality = 1 - (totalSlip / 0.3)
    reward += tractionQuality × tractionRewardMultiplier (0.05)
```
- **Purpose**: Reward smooth, controlled driving
- **Range**: 0 to +0.05 (perfect traction)
- **Example**: totalSlip = 0.15 → tractionQuality = 0.5 → +0.025 reward

#### 6. Wheel Event Penalties
```
IF anyWheelLocked:
    reward -= wheelEventPenalty (0.2)

IF anyWheelSpinning:
    reward -= wheelEventPenalty × 0.5 (0.1)
```
- **Purpose**: Discourage wheel lock/spin events
- **Range**: -0.2 (lock) or -0.1 (spin) per event
- **Note**: Both can occur simultaneously

#### 7. Obstacle Avoidance Penalty
```
minDistance = min(ray.Distances)  // Normalized 0-1
IF minDistance < obstacleThreshold (0.2):
    penalty = (0.2 - minDistance) / 0.2 × obstaclePenaltyMultiplier (0.5)
    reward -= penalty
```
- **Purpose**: Keep safe distance from walls/obstacles
- **Range**: 0 to -0.5 (at minDistance = 0)
- **Example**: minDistance = 0.1 → penalty = 0.25 → -0.25 reward


### Episode Termination Rewards

#### 9. Crash Penalty
```
IF minDistance < crashThreshold (0.05):
    reward -= 1.0
    EndEpisode()
```
- **Purpose**: Severe penalty for crashes
- **Range**: -1.0 (episode ends)

### Checkpoint System Rewards (Event-Based)

#### 10. Checkpoint Rewards
```
IF correctCheckpoint:
    reward += 0.2  // Correct checkpoint hit
    IF lap > 0:
        reward += 1.0  // Lap completion bonus
ELSE:
    reward -= 0.3  // Wrong checkpoint (backwards/skipped)
```
- **Purpose**: Guide agent through track in correct order
- **Range**: +0.2 (checkpoint), +1.2 (checkpoint + lap), or -0.3 (wrong)

---

## Complete Per-Step Reward Formula

```
R_step = R_speed + R_progress + R_track + R_slip + R_traction + R_wheels + R_obstacle + R_grounded

WHERE:
R_speed = (forwardSpeed > 0.5) ? forwardSpeed × 0.01 : 0
R_progress = (forwardSpeed > 0) ? forwardSpeed × 0.02 : 0
R_track = (wheelsOnTrack) ? 0.01 : -1.0
R_slip = -(|latSlip| + |longSlip|) × 0.1
R_traction = (forwardSpeed > 0.5 AND totalSlip < 0.3) ? (1 - totalSlip/0.3) × 0.05 : 0
R_wheels = -(anyWheelLocked ? 0.2 : 0) - (anyWheelSpinning ? 0.1 : 0)
R_obstacle = (minDistance < 0.2) ? -((0.2 - minDistance) / 0.2) × 0.5 : 0
R_grounded = (isGrounded) ? 0.01 : -0.1
```

---

## Scenario Simulations

### Simulation Parameters
- **Time Step**: 0.02s (50 Hz, typical Unity FixedUpdate)
- **Steps per Second**: 50 steps/sec
- **Episode Duration**: Variable (until crash or max steps)

---

### Scenario 1: Perfect Driving (Ideal Performance)
**Description**: Agent drives smoothly on track at good speed with perfect traction.

**Per-Step Conditions**:
- `forwardSpeed`: 25 m/s (90 km/h)
- `wheelsOnTrack`: true
- `totalSlip`: 0.1 (low slip, good traction)
- `anyWheelLocked`: false
- `anyWheelSpinning`: false
- `minDistance`: 0.5 (safe distance from obstacles)
- `isGrounded`: true

**Per-Step Reward Calculation**:
```
R_speed = 25 × 0.01 = +0.25
R_progress = 25 × 0.02 = +0.50
R_track = +0.01
R_slip = -0.1 × 0.1 = -0.01
R_traction = (1 - 0.1/0.3) × 0.05 = 0.033
R_wheels = 0
R_obstacle = 0
R_grounded = +0.01

R_step = 0.25 + 0.50 + 0.01 - 0.01 + 0.033 + 0 + 0 + 0.01 = +0.793
```

**Cumulative Reward**:
- **Per Second**: 0.793 × 50 = **+39.65 reward/sec**
- **10 seconds**: **+396.5**
- **30 seconds**: **+1,189.5**
- **1 minute**: **+2,379**
- **With 1 checkpoint**: +396.5 + 0.2 = **+396.7** (10 sec)
- **With 1 lap completion**: +396.5 + 0.2 + 1.0 = **+397.7** (10 sec)

---

### Scenario 2: Moderate Performance (Realistic)
**Description**: Agent drives at moderate speed with occasional minor slip.

**Per-Step Conditions**:
- `forwardSpeed`: 15 m/s (54 km/h)
- `wheelsOnTrack`: true
- `totalSlip`: 0.25 (moderate slip)
- `anyWheelLocked`: false
- `anyWheelSpinning`: false
- `minDistance`: 0.3 (moderate distance)
- `isGrounded`: true

**Per-Step Reward Calculation**:
```
R_speed = 15 × 0.01 = +0.15
R_progress = 15 × 0.02 = +0.30
R_track = +0.01
R_slip = -0.25 × 0.1 = -0.025
R_traction = (1 - 0.25/0.3) × 0.05 = 0.008
R_wheels = 0
R_obstacle = 0
R_grounded = +0.01

R_step = 0.15 + 0.30 + 0.01 - 0.025 + 0.008 + 0 + 0 + 0.01 = +0.453
```

**Cumulative Reward**:
- **Per Second**: 0.453 × 50 = **+22.65 reward/sec**
- **10 seconds**: **+226.5**
- **30 seconds**: **+679.5**
- **1 minute**: **+1,359**

---

### Scenario 3: Poor Performance (High Slip, Low Speed)
**Description**: Agent struggles with control, high slip, low speed.

**Per-Step Conditions**:
- `forwardSpeed`: 5 m/s (18 km/h)
- `wheelsOnTrack`: true
- `totalSlip`: 0.8 (high slip)
- `anyWheelLocked`: false
- `anyWheelSpinning`: true (occasional)
- `minDistance`: 0.25 (getting closer to walls)
- `isGrounded`: true

**Per-Step Reward Calculation**:
```
R_speed = 5 × 0.01 = +0.05
R_progress = 5 × 0.02 = +0.10
R_track = +0.01
R_slip = -0.8 × 0.1 = -0.08
R_traction = 0 (totalSlip > 0.3, no traction reward)
R_wheels = -0.1 (spinning)
R_obstacle = 0 (minDistance > 0.2)
R_grounded = +0.01

R_step = 0.05 + 0.10 + 0.01 - 0.08 + 0 - 0.1 + 0 + 0.01 = -0.01
```

**Cumulative Reward**:
- **Per Second**: -0.01 × 50 = **-0.5 reward/sec**
- **10 seconds**: **-5.0**
- **30 seconds**: **-15.0**
- **1 minute**: **-30.0**

---

### Scenario 4: Off-Track Penalty (Major Issue)
**Description**: Agent goes off track but continues moving.

**Per-Step Conditions**:
- `forwardSpeed`: 10 m/s
- `wheelsOnTrack`: false
- `totalSlip`: 0.5
- `anyWheelLocked`: false
- `anyWheelSpinning`: false
- `minDistance`: 0.4
- `isGrounded`: true

**Per-Step Reward Calculation**:
```
R_speed = 10 × 0.01 = +0.10
R_progress = 10 × 0.02 = +0.20
R_track = -1.0  // MAJOR PENALTY
R_slip = -0.5 × 0.1 = -0.05
R_traction = 0
R_wheels = 0
R_obstacle = 0
R_grounded = +0.01

R_step = 0.10 + 0.20 - 1.0 - 0.05 + 0 + 0 + 0 + 0.01 = -0.74
```

**Cumulative Reward**:
- **Per Second**: -0.74 × 50 = **-37.0 reward/sec**
- **5 seconds off-track**: **-185.0**
- **10 seconds off-track**: **-370.0**

---

### Scenario 5: Near-Crash Situation
**Description**: Agent gets dangerously close to obstacles.

**Per-Step Conditions**:
- `forwardSpeed`: 20 m/s
- `wheelsOnTrack`: true
- `totalSlip`: 0.2
- `anyWheelLocked`: false
- `anyWheelSpinning`: false
- `minDistance`: 0.1 (very close to wall)
- `isGrounded`: true

**Per-Step Reward Calculation**:
```
R_speed = 20 × 0.01 = +0.20
R_progress = 20 × 0.02 = +0.40
R_track = +0.01
R_slip = -0.2 × 0.1 = -0.02
R_traction = (1 - 0.2/0.3) × 0.05 = 0.017
R_wheels = 0
R_obstacle = -((0.2 - 0.1) / 0.2) × 0.5 = -0.25
R_grounded = +0.01

R_step = 0.20 + 0.40 + 0.01 - 0.02 + 0.017 - 0.25 + 0 + 0.01 = +0.357
```

**Cumulative Reward**:
- **Per Second**: 0.357 × 50 = **+17.85 reward/sec**
- **10 seconds**: **+178.5**

**If Crash Occurs**:
- Final step: +0.357 - 1.0 (crash penalty) = **-0.643**
- **10 seconds + crash**: +178.5 - 0.643 = **+177.857**

---

### Scenario 6: Wheel Lock Event
**Description**: Agent brakes too hard, causing wheel lock.

**Per-Step Conditions**:
- `forwardSpeed`: 12 m/s
- `wheelsOnTrack`: true
- `totalSlip`: 0.6
- `anyWheelLocked`: true
- `anyWheelSpinning`: false
- `minDistance`: 0.35
- `isGrounded`: true

**Per-Step Reward Calculation**:
```
R_speed = 12 × 0.01 = +0.12
R_progress = 12 × 0.02 = +0.24
R_track = +0.01
R_slip = -0.6 × 0.1 = -0.06
R_traction = 0
R_wheels = -0.2 (locked)
R_obstacle = 0
R_grounded = +0.01

R_step = 0.12 + 0.24 + 0.01 - 0.06 - 0.2 + 0 + 0 + 0.01 = +0.12
```

**Cumulative Reward**:
- **Per Second**: 0.12 × 50 = **+6.0 reward/sec**
- **10 seconds**: **+60.0**

---

### Scenario 7: Airborne (Car Jumps)
**Description**: Agent goes airborne (all wheels off ground).

**Per-Step Conditions**:
- `forwardSpeed`: 18 m/s
- `wheelsOnTrack`: false (can't be on track if airborne)
- `totalSlip`: 0.0 (no wheel contact)
- `anyWheelLocked`: false
- `anyWheelSpinning`: false
- `minDistance`: 0.4
- `isGrounded`: false

**Per-Step Reward Calculation**:
```
R_speed = 18 × 0.01 = +0.18
R_progress = 18 × 0.02 = +0.36
R_track = -1.0  // Off track
R_slip = 0 (no slip if no contact)
R_traction = 0
R_wheels = 0
R_obstacle = 0
R_grounded = -0.1

R_step = 0.18 + 0.36 - 1.0 + 0 + 0 + 0 + 0 - 0.1 = -0.56
```

**Cumulative Reward**:
- **Per Second**: -0.56 × 50 = **-28.0 reward/sec**
- **1 second airborne**: **-28.0**
- **2 seconds airborne**: **-56.0**

---

### Scenario 8: Complete Lap (Best Case)
**Description**: Agent completes a full lap with good performance.

**Assumptions**:
- Lap time: 30 seconds
- 5 checkpoints per lap
- Performance: Scenario 1 (perfect driving)
- All checkpoints hit correctly

**Reward Calculation**:
```
Base driving reward (30 sec): 39.65 × 30 = +1,189.5
Checkpoint rewards: 5 × 0.2 = +1.0
Lap completion bonus: +1.0

Total = 1,189.5 + 1.0 + 1.0 = +1,191.5
```

**Per Lap**: **+1,191.5 reward**

---

### Scenario 9: Crash Early in Episode
**Description**: Agent crashes after 5 seconds of moderate driving.

**Assumptions**:
- 5 seconds of Scenario 2 performance
- Then crash occurs

**Reward Calculation**:
```
Base driving (5 sec): 22.65 × 5 = +113.25
Crash penalty: -1.0

Total = 113.25 - 1.0 = +112.25
```

**Episode Total**: **+112.25 reward**

---

### Scenario 10: Mixed Performance Episode
**Description**: Realistic episode with various behaviors.

**Timeline**:
- 0-10s: Perfect driving (Scenario 1)
- 10-15s: Moderate performance (Scenario 2)
- 15-17s: Near-crash situation (Scenario 5)
- 17-20s: Wheel lock event (Scenario 6)
- 20-25s: Perfect driving (Scenario 1)
- 25s: Hits checkpoint (+0.2)
- 25-30s: Perfect driving (Scenario 1)
- 30s: Completes lap (+1.0)

**Reward Calculation**:
```
0-10s:   39.65 × 10 = +396.5
10-15s:  22.65 × 5  = +113.25
15-17s:  17.85 × 2  = +35.7
17-20s:  6.0 × 3    = +18.0
20-25s:  39.65 × 5  = +198.25
Checkpoint:         = +0.2
25-30s:  39.65 × 5  = +198.25
Lap bonus:          = +1.0

Total = 396.5 + 113.25 + 35.7 + 18.0 + 198.25 + 0.2 + 198.25 + 1.0 = +961.15
```

**Episode Total**: **+961.15 reward**

---

## Reward Scale Analysis

### Reward Ranges per Step:
- **Best Case**: ~+0.79 (perfect driving at 25 m/s)
- **Good Case**: ~+0.45 (moderate performance)
- **Neutral**: ~0.0 (low speed, high slip)
- **Poor Case**: ~-0.01 to -0.74 (off-track, crashes)

### Expected Episode Rewards:
- **Excellent Episode** (30s perfect lap): **+1,191.5**
- **Good Episode** (30s moderate): **+679.5**
- **Poor Episode** (crashes early): **+112.25**
- **Bad Episode** (off-track): **-370.0** (10s off-track)

### Key Insights:
1. **Off-track penalty (-1.0) is severe** - 100x larger than on-track reward
2. **Speed rewards dominate** - Forward speed contributes most to positive rewards
3. **Traction rewards are small** - Only +0.05 max, but help fine-tune behavior
4. **Checkpoint system provides guidance** - +0.2 per checkpoint, +1.0 per lap
5. **Crash penalty is significant** - -1.0 ends episode, but doesn't wipe out good performance

### Algorithm Performance Check:
- **Positive rewards** should accumulate during good driving
- **Negative rewards** should quickly signal problems (off-track, crashes)
- **Reward scale** is balanced: good driving ~+40/sec, bad driving ~-0.5 to -37/sec
- **Checkpoint rewards** provide intermediate goals and lap completion incentives

---

## Recommendations for Tuning

### If agent is too slow:
- Increase `speedRewardMultiplier` (currently 0.01)
- Increase `forwardProgressMultiplier` (currently 0.02)

### If agent crashes too often:
- Increase `obstaclePenaltyMultiplier` (currently 0.5)
- Decrease `crashThreshold` (currently 0.05) to terminate earlier

### If agent goes off-track:
- The -1.0 penalty is already very large
- Consider adding progressive off-track penalty (increases over time)

### If training is unstable:
- Check reward scale: current range is reasonable (-1.0 to +0.79 per step)
- Consider reward normalization or clipping
- Monitor cumulative rewards: should see positive trend over episodes

