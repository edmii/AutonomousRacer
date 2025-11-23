# Quick script to check saved model status
# Run this to see what models were saved and their training progress

Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "Checking Saved Model Status" -ForegroundColor Green
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""

$resultsPath = "MLAgentsEnv\mlagents-env\Scripts\results\car_v0"

if (-not (Test-Path $resultsPath)) {
    Write-Host "ERROR: No training results found at: $resultsPath" -ForegroundColor Red
    Write-Host "You may need to start training first." -ForegroundColor Yellow
    exit 1
}

# Check for ONNX models
$onnxModels = Get-ChildItem "$resultsPath\CarAgentParams\*.onnx" -ErrorAction SilentlyContinue

if ($onnxModels) {
    Write-Host "✓ Found saved models!" -ForegroundColor Green
    Write-Host ""
    
    # Get latest model
    $latestModel = $onnxModels | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    Write-Host "Latest Model:" -ForegroundColor Yellow
    Write-Host "  File: $($latestModel.Name)" -ForegroundColor White
    Write-Host "  Saved: $($latestModel.LastWriteTime)" -ForegroundColor White
    
    # Extract step count from filename
    if ($latestModel.Name -match '(\d+)') {
        $steps = [int]$matches[1]
        Write-Host "  Training Steps: $steps" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host 'All Checkpoints (sorted by date):' -ForegroundColor Yellow
    $onnxModels | Sort-Object LastWriteTime -Descending | ForEach-Object {
        $stepCount = if ($_.Name -match '(\d+)') { [int]$matches[1] } else { 0 }
        $stepText = "$stepCount steps"
        Write-Host "  - $($_.Name) ($stepText) - $($_.LastWriteTime)" -ForegroundColor Gray
    }
    
    Write-Host ""
    
    # Check main model file
    $mainModel = Join-Path $resultsPath "CarAgentParams.onnx"
    if (Test-Path $mainModel) {
        $mainModelInfo = Get-Item $mainModel
        Write-Host "Main Model File:" -ForegroundColor Yellow
        Write-Host "  $mainModel" -ForegroundColor White
        Write-Host "  Last Updated: $($mainModelInfo.LastWriteTime)" -ForegroundColor White
    }
    
} else {
    Write-Host "⚠ No ONNX models found in: $resultsPath\CarAgentParams\" -ForegroundColor Yellow
}

Write-Host ""

# Check training status
$statusFile = Join-Path $resultsPath "run_logs\training_status.json"
if (Test-Path $statusFile) {
    Write-Host "Training Status:" -ForegroundColor Yellow
    try {
        $status = Get-Content $statusFile | ConvertFrom-Json
        if ($status.CarAgentParams) {
            if ($status.CarAgentParams.final_checkpoint) {
                $final = $status.CarAgentParams.final_checkpoint
                Write-Host "  Final Checkpoint: $($final.steps) steps" -ForegroundColor White
                Write-Host "  Reward: $($final.reward)" -ForegroundColor White
            }
            if ($status.CarAgentParams.checkpoints) {
                Write-Host "  Total Checkpoints: $($status.CarAgentParams.checkpoints.Count)" -ForegroundColor White
            }
        }
    } catch {
        Write-Host "  Could not parse training status file" -ForegroundColor Yellow
    }
}

Write-Host ""

# Check checkpoint file for resuming
$checkpointFile = Join-Path $resultsPath "CarAgentParams\checkpoint.pt"
if (Test-Path $checkpointFile) {
    Write-Host "✓ Checkpoint file found - you can resume training!" -ForegroundColor Green
    Write-Host "  Run: .\train_model_resume.ps1" -ForegroundColor Cyan
} else {
    Write-Host "⚠ No checkpoint.pt found - cannot resume training" -ForegroundColor Yellow
    Write-Host "  You can start new training with: .\train_model.ps1" -ForegroundColor Cyan
}

Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Green
Write-Host "  1. Test model in Unity: See CHECK_MODEL_AND_TEST.md" -ForegroundColor White
Write-Host "  2. Resume training: .\train_model_resume.ps1" -ForegroundColor White
Write-Host "  3. View training graphs: tensorboard --logdir=results" -ForegroundColor White
Write-Host ("=" * 60) -ForegroundColor Cyan

