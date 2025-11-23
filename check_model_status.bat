@echo off
REM Quick batch script to check saved model status
REM This avoids PowerShell execution policy issues

echo ============================================================
echo Checking Saved Model Status
echo ============================================================
echo.

set "RESULTS_PATH=MLAgentsEnv\mlagents-env\Scripts\results\car_v0"

if not exist "%RESULTS_PATH%" (
    echo ERROR: No training results found at: %RESULTS_PATH%
    echo You may need to start training first.
    pause
    exit /b 1
)

echo Checking for saved models...
echo.

REM Use PowerShell with bypass to list models
powershell -ExecutionPolicy Bypass -NoProfile -Command "& { $models = Get-ChildItem '%RESULTS_PATH%\CarAgentParams\*.onnx' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 5; if ($models) { Write-Host 'Latest 5 saved models:' -ForegroundColor Green; $models | ForEach-Object { $step = if ($_.Name -match '(\d+)') { $matches[1] } else { '?' }; Write-Host \"  $($_.Name) - $step steps - $($_.LastWriteTime)\" } } else { Write-Host 'No models found!' -ForegroundColor Yellow } }"

echo.
echo Checking for main model file...
if exist "%RESULTS_PATH%\CarAgentParams.onnx" (
    echo [OK] Main model file exists: CarAgentParams.onnx
) else (
    echo [WARNING] Main model file not found
)

echo.
echo Checking for checkpoint file (for resuming)...
if exist "%RESULTS_PATH%\CarAgentParams\checkpoint.pt" (
    echo [OK] Checkpoint file found - you can resume training!
    echo        Run: train_model_resume.bat
) else (
    echo [WARNING] No checkpoint.pt found - cannot resume training
    echo           You can start new training with: train_model.bat
)

echo.
echo ============================================================
echo Next Steps:
echo   1. Test model in Unity: See CHECK_MODEL_AND_TEST.md
echo   2. Resume training: train_model_resume.bat
echo   3. View training graphs: tensorboard --logdir=results
echo ============================================================
echo.
echo To run the PowerShell script directly, use:
echo   powershell -ExecutionPolicy Bypass -File check_model_status.ps1
echo.
pause
