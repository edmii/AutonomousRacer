@echo off
setlocal EnableDelayedExpansion

echo ========================================================
echo        ML-Agents Model Cleanup Tool
echo ========================================================
echo.
echo This script will keep the 5 most recent checkpoints and DELETE the rest.
echo It will NOT delete 'checkpoint.pt' or the main 'CarAgentParams.onnx'.
echo.
echo Target Directory: MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams
echo.

set "TARGET_DIR=MLAgentsEnv\mlagents-env\Scripts\results\car_v0\CarAgentParams"

if not exist "%TARGET_DIR%" (
    echo ERROR: Directory not found!
    pause
    exit /b 1
)

cd /d "%TARGET_DIR%"

echo [1/2] Cleaning up .ONNX files (Keeping newest 5)...
set count=0
for /f "skip=5 delims=" %%F in ('dir CarAgentParams-*.onnx /O-D /B') do (
    echo Deleting: %%F
    del "%%F"
    set /a count+=1
)
echo Deleted %count% old .onnx files.
echo.

echo [2/2] Cleaning up .PT files (Keeping newest 5)...
set count=0
for /f "skip=5 delims=" %%F in ('dir CarAgentParams-*.pt /O-D /B') do (
    echo Deleting: %%F
    del "%%F"
    set /a count+=1
)
echo Deleted %count% old .pt files.
echo.

echo ========================================================
echo Cleanup Complete!
echo.
echo Remaining space freed up.
echo.
pause

