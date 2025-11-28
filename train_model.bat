@echo off
REM Training script for Autonomous Racer ML-Agents
REM Make sure Unity is running with the environment before starting training

cd MLAgentsEnv\mlagents-env\Scripts

echo Checking CUDA availability...
python ..\..\..\check_cuda.py
if errorlevel 1 (
    echo Warning: Could not check CUDA. Continuing anyway...
)
echo.

echo Starting ML-Agents training...
echo Config: Config\Car_ppo.yaml
echo Results will be saved to: results\car_v0
echo.
echo NOTE: ML-Agents will automatically use CUDA if available, otherwise CPU.
echo.
echo For parallel training (faster), use train_model_parallel.bat
echo or add --num-envs=4 --env-path=path\to\build.exe
echo.

if exist "results\car_v3" (
    echo.
    echo Found existing training data at results\car_v3.
    echo Deleting old data to ensure a fresh training session...
    rmdir /s /q "results\car_v3"
    echo Old data deleted.
    echo.
)

call mlagents-learn Config\Car_ppo.yaml --run-id=car_v3 --force

pause

