@echo off
REM Training script for Autonomous Racer ML-Agents with PARALLEL ENVIRONMENTS
REM This uses multiple Unity instances for faster training
REM 
REM IMPORTANT: This requires Unity BUILDS (executables), not the Editor
REM You need to build your Unity project first, then specify the path

cd MLAgentsEnv\mlagents-env\Scripts

echo Checking CUDA availability...
python ..\..\..\check_cuda.py
if errorlevel 1 (
    echo Warning: Could not check CUDA. Continuing anyway...
)
echo.

echo Starting PARALLEL ML-Agents training...
echo Config: Config\Car_ppo.yaml
echo Results will be saved to: results\car_v0
echo.
echo Using 4 parallel environments for faster training
echo NOTE: This requires Unity BUILD, not Editor!
echo.

REM Change this path to your Unity build executable
REM Example: --env-path=..\..\..\Builds\AutonomousRacer.exe
REM If not set, will try to use Editor (may not work with num-envs > 1)

call mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-envs=4 --num-areas=1

pause

