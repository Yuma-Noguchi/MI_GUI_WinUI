@echo off
setlocal enabledelayedexpansion

echo Validating test infrastructure setup...
echo.

:: Check PowerShell is available
where pwsh >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo PowerShell Core (pwsh) is required but not found.
    echo Please install PowerShell Core from https://github.com/PowerShell/PowerShell
    exit /b 1
)

:: Check .NET SDK is available
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo .NET SDK is required but not found.
    echo Please install .NET SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

:: Create necessary directories
echo Creating test directories...
for %%d in (
    "TestData\Profiles"
    "TestData\Actions"
    "TestData\Config"
    "TestData\Prompts"
    "TestOutput"
) do (
    if not exist "%%d" (
        mkdir "%%d"
        echo Created: %%d
    )
)

:: Copy sample test data if not present
echo Checking test data files...
if not exist "TestData\Profiles\sample_profile.json" (
    copy "TestData\Samples\sample_profile.json" "TestData\Profiles\" >nul 2>nul
    if !ERRORLEVEL! neq 0 (
        echo Warning: Could not copy sample profile data
    )
)

if not exist "TestData\Actions\sample_action.json" (
    copy "TestData\Samples\sample_action.json" "TestData\Actions\" >nul 2>nul
    if !ERRORLEVEL! neq 0 (
        echo Warning: Could not copy sample action data
    )
)

:: Run verification script
echo.
echo Running verification tests...
pwsh -NoProfile -ExecutionPolicy Bypass -File verify_setup.ps1

if %ERRORLEVEL% equ 0 (
    echo.
    echo ===============================
    echo Test setup validation passed!
    echo ===============================
    echo.
    echo Next steps:
    echo 1. Review test results in TestResults directory
    echo 2. Check QUICKSTART.md for usage instructions
    echo 3. Run full test suite with: run_tests.ps1
) else (
    echo.
    echo ====================================
    echo Test setup validation failed!
    echo ====================================
    echo.
    echo Please check:
    echo 1. All required dependencies are installed
    echo 2. Test data files are present and valid
    echo 3. Build configuration is correct
    echo.
    echo See logs in TestResults directory for details
)

exit /b %ERRORLEVEL%