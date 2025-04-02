param (
    [switch]$NoRestore,
    [switch]$NoBuild,
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

# Set error action preference to stop on any error
$ErrorActionPreference = "Stop"

Write-Host "Starting test verification..." -ForegroundColor Cyan

# Check if solution exists
$solutionPath = "..\MI_GUI_WinUI.sln"
if (-not (Test-Path $solutionPath)) {
    Write-Error "Solution file not found at: $solutionPath"
    exit 1
}

# Create test output directory if it doesn't exist
$testOutputDir = ".\TestResults"
if (-not (Test-Path $testOutputDir)) {
    New-Item -ItemType Directory -Path $testOutputDir | Out-Null
}

# Ensure test data directories exist
$testDataDirs = @(
    ".\TestData\Profiles",
    ".\TestData\Actions",
    ".\TestData\Config",
    ".\TestData\Prompts"
)

foreach ($dir in $testDataDirs) {
    if (-not (Test-Path $dir)) {
        Write-Host "Creating directory: $dir" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

try {
    # Restore packages if needed
    if (-not $NoRestore) {
        Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
        dotnet restore $solutionPath
        if ($LASTEXITCODE -ne 0) {
            throw "Package restore failed"
        }
    }

    # Build solution if needed
    if (-not $NoBuild) {
        Write-Host "Building solution..." -ForegroundColor Cyan
        dotnet build $solutionPath --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
    }

    # Run verification tests
    Write-Host "Running verification tests..." -ForegroundColor Cyan
    dotnet test `
        --filter "FullyQualifiedName~Infrastructure.TestSetupVerification" `
        --configuration $Configuration `
        --logger "trx;LogFileName=TestSetupVerification.trx" `
        --results-directory $testOutputDir `
        --collect:"XPlat Code Coverage" `
        --no-build `
        --verbosity normal

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Test verification completed successfully!" -ForegroundColor Green
    }
    else {
        throw "Test verification failed"
    }
}
catch {
    Write-Error "Test verification failed: $_"
    exit 1
}

# Check test results
$testResultFile = Get-ChildItem -Path $testOutputDir -Filter "TestSetupVerification.trx" -Recurse | Select-Object -First 1
if ($testResultFile) {
    Write-Host "`nTest Results Summary:" -ForegroundColor Cyan
    [xml]$testResults = Get-Content $testResultFile.FullName
    
    $total = $testResults.TestRun.Results.UnitTestResult.Count
    $passed = ($testResults.TestRun.Results.UnitTestResult | Where-Object outcome -eq "Passed").Count
    $failed = ($testResults.TestRun.Results.UnitTestResult | Where-Object outcome -eq "Failed").Count
    
    Write-Host "Total Tests: $total" -ForegroundColor White
    Write-Host "Passed: $passed" -ForegroundColor Green
    Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
}

# Report coverage if available
$coverageFile = Get-ChildItem -Path $testOutputDir -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1
if ($coverageFile) {
    Write-Host "`nCode Coverage Summary:" -ForegroundColor Cyan
    [xml]$coverage = Get-Content $coverageFile.FullName
    $lineRate = [math]::Round($coverage.coverage.'line-rate' * 100, 2)
    Write-Host "Line Coverage: $lineRate%" -ForegroundColor $(if ($lineRate -gt 80) { "Green" } else { "Yellow" })
}