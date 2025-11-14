# PowerShell training script for Autonomous Racer ML-Agents with PARALLEL ENVIRONMENTS
# This uses multiple Unity instances for faster training
# 
# IMPORTANT: This requires Unity BUILDS (executables), not the Editor
# You need to build your Unity project first, then specify the path

$ErrorActionPreference = "Stop"

Write-Host "Checking CUDA availability..." -ForegroundColor Cyan
python check_cuda.py
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not check CUDA. Continuing anyway..." -ForegroundColor Yellow
}
Write-Host ""

Set-Location "MLAgentsEnv\mlagents-env\Scripts"

Write-Host "Starting PARALLEL ML-Agents training..." -ForegroundColor Green
Write-Host "Config: Config\Car_ppo.yaml" -ForegroundColor Cyan
Write-Host "Results will be saved to: results\car_v0" -ForegroundColor Cyan
Write-Host ""
Write-Host "Using 4 parallel environments for faster training" -ForegroundColor Yellow
Write-Host "NOTE: This requires Unity BUILD, not Editor!" -ForegroundColor Yellow
Write-Host ""

# Change this path to your Unity build executable
# Example: --env-path=..\..\..\Builds\AutonomousRacer.exe
# If not set, will try to use Editor (may not work with num-envs > 1)

$envPath = "..\..\..\Builds\AutonomousRacer.exe"

if (Test-Path $envPath) {
    Write-Host "Using Unity build: $envPath" -ForegroundColor Green
    & mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-envs=4 --env-path=$envPath
} else {
    Write-Host "WARNING: Unity build not found at: $envPath" -ForegroundColor Red
    Write-Host "Please update the `$envPath variable in this script to point to your Unity build." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Attempting with Editor (may not work with num-envs > 1)..." -ForegroundColor Yellow
    & mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force --num-envs=4
}

Write-Host ""
Write-Host "Training completed. Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

