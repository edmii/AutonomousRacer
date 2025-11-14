# PowerShell script to RESUME previous training session
# This continues training from the last checkpoint

$ErrorActionPreference = "Stop"

Write-Host "Checking CUDA availability..." -ForegroundColor Cyan
python check_cuda.py
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Could not check CUDA. Continuing anyway..." -ForegroundColor Yellow
}
Write-Host ""

Set-Location "MLAgentsEnv\mlagents-env\Scripts"

# Check if previous training exists
$resultsPath = "results\car_v0"
if (-not (Test-Path $resultsPath)) {
    Write-Host "ERROR: No previous training found at: $resultsPath" -ForegroundColor Red
    Write-Host "Use train_model.ps1 to start a new training session." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Write-Host "Resuming ML-Agents training..." -ForegroundColor Green
Write-Host "Config: Config\Car_ppo.yaml" -ForegroundColor Cyan
Write-Host "Run ID: car_v0" -ForegroundColor Cyan
Write-Host "Results directory: $resultsPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTE: Training will continue from the last checkpoint." -ForegroundColor Yellow
Write-Host "NOTE: ML-Agents will automatically use CUDA if available, otherwise CPU." -ForegroundColor Yellow
Write-Host ""

# Resume training (--resume flag loads the checkpoint automatically)
& mlagents-learn Config\Car_ppo.yaml --run-id=car_v0 --resume

Write-Host ""
Write-Host "Training completed. Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

