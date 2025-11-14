# Fix: NWH Vehicle Controller "Auto Set Input" Overriding Agent Inputs

## The Problem

Your NWH Vehicle Controller has **"Auto Set Input" enabled**. This setting:
- Automatically gets input from InputProviders in the scene
- **Overrides any input set from external scripts** (including your agent!)
- This is why the car shows Steering: 0, Throttle: 0, Brakes: 0 even when the agent tries to send actions

## The Solution

**Disable "Auto Set Input"** so your agent can control the vehicle:

1. Select your car GameObject in Unity
2. Find the **Vehicle Controller** component (NWH Vehicle Physics 2)
3. Click the **"Input"** tab
4. **Uncheck "Auto Set Input"**
5. This allows your `CarAgent.ApplyVehicleInputs()` method to set inputs directly

## Why This Happens

When "Auto Set Input" is enabled:
- NWH Vehicle Controller looks for InputProviders in the scene
- It tries to read from Unity's Input System
- Since you're using the new Input System, it may be causing conflicts
- More importantly, it **overrides** any values your agent script tries to set

When "Auto Set Input" is disabled:
- The agent can directly set Steering, Throttle, and Brakes via reflection
- Your `ApplyVehicleInputs()` method will work correctly
- The car will respond to agent actions

## Verification

After disabling "Auto Set Input":
1. Press Play in Unity
2. Start training or run the verification script
3. Check the Vehicle Controller's "Input States" - you should see:
   - Steering values changing (not always 0)
   - Throttle values when the agent accelerates
   - Brake values when the agent brakes
4. The car should start moving!

## Additional Notes

- The "Swap Input In Reverse" setting can stay enabled if you want
- Make sure your `CarAgent` component has the `nwhVehicleController` reference assigned in the Inspector
- The `ApplyVehicleInputs()` method will use Method 1 (NWH Vehicle Controller) when this is set up correctly

