# PowerShell training script for Autonomous Racer ML-Agents
# Make sure Unity is running with the environment before starting training

$ErrorActionPreference = "Stop"

Write-Host "Checking CUDA availability..." -ForegroundColor Cyan
python check_cuda.py
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not check CUDA. Continuing anyway..." -ForegroundColor Yellow
}
Write-Host ""

Set-Location "MLAgentsEnv\mlagents-env\Scripts"

Write-Host "Starting ML-Agents training..." -ForegroundColor Green
Write-Host "Config: Config\Car_ppo.yaml" -ForegroundColor Cyan
Write-Host "Results will be saved to: results\car_v0" -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTE: ML-Agents will automatically use CUDA if available, otherwise CPU." -ForegroundColor Yellow
Write-Host ""
Write-Host "For parallel training (faster), use train_model_parallel.ps1" -ForegroundColor Yellow
Write-Host "or add --num-envs=4 --env-path=path\to\build.exe" -ForegroundColor Yellow
Write-Host ""

& mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --force

Write-Host ""
Write-Host "Training completed. Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

