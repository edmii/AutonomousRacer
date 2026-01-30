@echo off
REM Training script to RESUME previous training session
REM This continues training from the last checkpoint

cd MLAgentsEnv\mlagents-env\Scripts

echo Checking CUDA availability...
python ..\..\..\check_cuda.py
if errorlevel 1 (
    echo Warning: Could not check CUDA. Continuing anyway...
)
echo.

REM Check if previous training exists
if not exist "results\car_v11" (
    echo ERROR: No previous training found at results\car_v11
    echo Use train_model.bat to start a new training session.
    pause
    exit /b 1
)

echo Resuming ML-Agents training...
echo Config: Config\Car_ppo.yaml
echo Run ID: car_v11
echo.
echo NOTE: Training will continue from the last checkpoint.
echo NOTE: ML-Agents will automatically use CUDA if available, otherwise CPU.
echo.

REM Resume training (--resume flag loads the checkpoint automatically)
call mlagents-learn Config\Car_ppo.yaml --run-id=car_v11 --resume

pause

