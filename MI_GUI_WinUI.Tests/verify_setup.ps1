#!/usr/bin/env pwsh

param (
    [switch]$NoRestore,
    [switch]$NoBuild,
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "Starting test infrastructure verification..." -ForegroundColor Cyan

# Create test data structure
$testDataDirs = @(
    "TestData\Profiles",
    "TestData\Actions",
    "TestData\Config",
    "TestData\Prompts",
    "TestData\Samples",
    "TestData\Samples\Schemas"
)

foreach ($dir in $testDataDirs) {
    if (-not (Test-Path $dir)) {
        Write-Host "Creating directory: $dir" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# Ensure sample data exists
$requiredFiles = @{
    "TestData\Samples\sample_profile.json" = "Sample profile data"
    "TestData\Samples\sample_action.json" = "Sample action data"
    "TestData\Samples\Schemas\profile-schema.json" = "Profile schema"
    "TestData\Samples\Schemas\action-schema.json" = "Action schema"
}

$missingFiles = $false
foreach ($file in $requiredFiles.Keys) {
    if (-not (Test-Path $file)) {
        Write-Warning "Missing required file: $file ($($requiredFiles[$file]))"
        $missingFiles = $true
    }
}

if ($missingFiles) {
    Write-Error "Required test files are missing. Please ensure all sample data and schemas are present."
    exit 1
}

# Restore packages if needed
if (-not $NoRestore) {
    Write-Host "Restoring packages..." -ForegroundColor Yellow
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Package restore failed" }
}

# Build if needed
if (-not $NoBuild) {
    Write-Host "Building test project..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

# Run verification tests
Write-Host "Running verification tests..." -ForegroundColor Yellow

# First validate test data
Write-Host "Validating test data..." -ForegroundColor Yellow
$dataTests = dotnet test `
    --filter "FullyQualifiedName~Infrastructure.TestDataVerification" `
    --configuration $Configuration `
    --no-build `
    --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Test data validation failed!"
    exit 1
}

# Then run setup verification
Write-Host "Running setup verification..." -ForegroundColor Yellow
$setupTests = dotnet test `
    --filter "FullyQualifiedName~Infrastructure.TestSetupVerification" `
    --configuration $Configuration `
    --no-build `
    --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Setup verification failed!"
    exit 1
}

# Run smoke tests
Write-Host "Running smoke tests..." -ForegroundColor Yellow
$smokeTests = dotnet test `
    --filter "TestCategory=Smoke" `
    --configuration $Configuration `
    --no-build `
    --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nVerification completed successfully!" -ForegroundColor Green
    
    Write-Host "`nTest Environment Details:" -ForegroundColor Cyan
    Write-Host "  .NET SDK Version: $(dotnet --version)"
    Write-Host "  Configuration: $Configuration"
    Write-Host "  Platform: $Platform"
    Write-Host "  Test Directories:"
    
    foreach ($dir in $testDataDirs) {
        $exists = Test-Path $dir
        $status = if ($exists) { "Created" } else { "Missing" }
        Write-Host "    $dir : $status"
    }

    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Review test results in TestResults directory"
    Write-Host "2. Check QUICKSTART.md for usage instructions"
    Write-Host "3. Run full test suite with: run_tests.ps1"
}
else {
    Write-Error "Smoke tests failed!"
    Write-Host "`nTroubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Check test output above for specific failures"
    Write-Host "2. Verify all dependencies are installed"
    Write-Host "3. Check test data files exist and are valid"
    Write-Host "4. Review test implementation if tests are failing"
    exit 1
}