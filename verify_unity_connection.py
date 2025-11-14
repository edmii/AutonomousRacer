"""
Simple script to verify Unity ML-Agents environment connection
Run this to check if Unity is properly connected before training

Usage:
    cd MLAgentsEnv\mlagents-env\Scripts
    python ..\..\..\verify_unity_connection.py
"""
import sys
import os

# Get the script's directory and construct absolute path to site-packages
script_dir = os.path.dirname(os.path.abspath(__file__))
site_packages_path = os.path.join(script_dir, 'MLAgentsEnv', 'mlagents-env', 'Lib', 'site-packages')

# Add ML-Agents to path if it exists
if os.path.exists(site_packages_path):
    sys.path.insert(0, site_packages_path)
else:
    # Try alternative: if running from Scripts directory, go up
    alt_path = os.path.join(os.path.dirname(script_dir), 'MLAgentsEnv', 'mlagents-env', 'Lib', 'site-packages')
    if os.path.exists(alt_path):
        sys.path.insert(0, alt_path)
    else:
        print(f"Warning: Could not find ML-Agents site-packages at:")
        print(f"  {site_packages_path}")
        print(f"  {alt_path}")
        print("\nMake sure you're running from the project root or MLAgentsEnv\\mlagents-env\\Scripts directory")

try:
    # Note: Linter may show warnings here, but imports work at runtime due to path manipulation above
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.base_env import ActionTuple
    import numpy as np
    
    print("=" * 60)
    print("Unity ML-Agents Connection Verification")
    print("=" * 60)
    print("\n⚠ IMPORTANT: Before running this script:")
    print("  1. Open Unity and your scene")
    print("  2. Make sure ML-Agents Academy is in the scene")
    print("  3. Make sure your car GameObject has:")
    print("     - CarAgent component")
    print("     - Behavior Parameters component (Behavior Type = 'Default')")
    print("     - Decision Requester component")
    print("  4. Press Play in Unity")
    print("  5. Wait for Unity to show 'Waiting for connection...' in Console")
    print("\nAttempting to connect to Unity Editor (port 5004)...")
    print("Waiting for connection (this may take up to 60 seconds)...")
    
    # Connect to Unity Editor (file_name=None means connect to editor)
    # base_port=None will use DEFAULT_EDITOR_PORT (5004)
    try:
        env = UnityEnvironment(file_name=None, base_port=5004, seed=0, side_channels=[], timeout_wait=60)
    except Exception as e:
        print(f"\n✗ Connection failed: {e}")
        print("\nCommon issues:")
        print("  - Unity is not in Play mode")
        print("  - Unity Console shows errors")
        print("  - ML-Agents Academy component missing from scene")
        print("  - Port 5004 is blocked by firewall or another process")
        print("\nCheck Unity Console for 'Waiting for connection...' message")
        raise
    
    print("\n✓ Successfully connected to Unity environment!")
    print(f"✓ Behavior specs: {list(env.behavior_specs.keys())}")
    
    # Get behavior name (should be CarAgentParams)
    behavior_name = list(env.behavior_specs.keys())[0]
    behavior_spec = env.behavior_specs[behavior_name]
    
    print(f"\nBehavior Name: {behavior_name}")
    print(f"Observation Space: {behavior_spec.observation_specs}")
    print(f"Action Space: Continuous={behavior_spec.action_spec.continuous_size}, Discrete={behavior_spec.action_spec.discrete_size}")
    
    # Reset environment
    env.reset()
    decision_steps, terminal_steps = env.get_steps(behavior_name)
    
    if len(decision_steps) > 0:
        print(f"\n✓ Environment reset successful!")
        print(f"✓ Number of agents: {len(decision_steps)}")
        print(f"✓ Observation shape: {decision_steps.obs[0].shape}")
        
        # Send a test action
        print(f"\nSending test action (steer=0.5, throttle=0.8)...")
        action_tuple = ActionTuple()
        action_tuple.add_continuous(np.array([[0.5, 0.8]]))  # [steer, throttle]
        env.set_actions(behavior_name, action_tuple)
        env.step()
        
        print("✓ Action sent successfully!")
        print("\nCheck Unity Console for:")
        print("  - [CarAgent] Action logs showing the received actions")
        print("  - [CarAgent] Applied via... logs showing which input method was used")
        print("  - [CarAgent] Status logs showing environment state")
        
    else:
        print("\n⚠ Warning: No agents found in environment!")
        print("Make sure the car agent is active in the scene.")
    
    env.close()
    print("\n" + "=" * 60)
    print("Connection test completed successfully!")
    print("=" * 60)
    print("\nIf you see action logs in Unity Console, the connection is working.")
    print("If not, check:")
    print("  1. Unity is in Play mode")
    print("  2. Behavior Parameters > Behavior Type is set to 'Default'")
    print("  3. Decision Requester component is added to the car")
    print("  4. CarAgent component has enableDebugLogging = true")
    
except ImportError as e:
    print(f"✗ Error importing ML-Agents: {e}")
    print("Make sure you're running this from the project root directory.")
    sys.exit(1)
    
except Exception as e:
    error_msg = str(e)
    print(f"\n✗ Error connecting to Unity: {error_msg}")
    
    if "timeout" in error_msg.lower() or "took too long" in error_msg.lower():
        print("\n⚠ TIMEOUT ERROR - Unity didn't respond in time")
        print("\nChecklist:")
        print("  ✓ Unity is in Play mode (not paused)")
        print("  ✓ Unity Console shows 'Waiting for connection...' or 'Connected new brain'")
        print("  ✓ ML-Agents Academy component exists in the scene")
        print("  ✓ No errors in Unity Console")
        print("  ✓ Car GameObject is active in the scene")
        print("  ✓ Behavior Parameters > Behavior Type is set to 'Default' (not 'Heuristic Only')")
    elif "port" in error_msg.lower() or "address" in error_msg.lower():
        print("\n⚠ PORT/CONNECTION ERROR")
        print("  - Port 5004 might be blocked or in use")
        print("  - Try closing other Unity instances")
        print("  - Check Windows Firewall settings")
    else:
        print("\nTroubleshooting:")
        print("  1. Unity is in Play mode (not paused)")
        print("  2. Check Unity Console for errors or connection messages")
        print("  3. Verify Behavior Parameters > Behavior Name matches config (CarAgentParams)")
        print("  4. Make sure ML-Agents Academy component is in the scene")
        print("  5. Try stopping Play mode, then starting it again")
        print("  6. Restart Unity and try again")
    
    print("\nNext steps:")
    print("  - Look at Unity Console for any error messages")
    print("  - Verify the scene has an ML-Agents Academy GameObject")
    print("  - Make sure the car agent is active and has all required components")
    sys.exit(1)

