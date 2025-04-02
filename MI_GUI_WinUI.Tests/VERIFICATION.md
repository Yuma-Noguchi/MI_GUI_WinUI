# Test Infrastructure Verification Checklist

## Initial Setup

1. Run verification script:
```powershell
.\verify_setup.ps1
```

This will:
- Create required directories
- Validate test data
- Run verification tests
- Check smoke tests

## Manual Verification Steps

### 1. Check Project Structure
- [ ] Solution includes MI_GUI_WinUI.Tests project
- [ ] All required package references are resolved
- [ ] Project builds successfully

### 2. Verify Test Data
- [ ] TestData directory exists with subdirectories:
  - [ ] Profiles/
  - [ ] Actions/
  - [ ] Config/
  - [ ] Prompts/
  - [ ] Samples/
  - [ ] Schemas/
- [ ] Sample files are present and valid

### 3. Verify Test Categories
Run specific test categories:
```powershell
# Smoke tests
dotnet test --filter "TestCategory=Smoke"

# Integration tests
dotnet test --filter "TestCategory=Integration"

# Performance tests
dotnet test --filter "TestCategory=Performance"
```

### 4. Verify Schema Validation
- [ ] Profile schema validates sample profile
- [ ] Action schema validates sample action
- [ ] Custom test data validates successfully

### 5. Check Test Results
- [ ] Test results directory created
- [ ] Coverage reports generated
- [ ] Test logs available

## Troubleshooting Common Issues

### Missing Dependencies
```powershell
dotnet restore
```

### Schema Validation Failures
1. Check schema files in TestData/Samples/Schemas/
2. Validate JSON files against schemas
3. Use SchemaValidator.GetValidationErrors() for details

### Test Data Issues
1. Verify file permissions
2. Check file formats
3. Validate against schemas

### Build Errors
1. Check package versions
2. Verify project references
3. Clean and rebuild solution

## Validation Commands

### Quick Validation
```powershell
.\verify_setup.ps1 -NoRestore
```

### Full Validation
```powershell
.\verify_setup.ps1
```

### Run All Tests
```powershell
.\run_tests.ps1
```

## Required Configurations

### Visual Studio
- Windows Desktop Development workload
- .NET Multi-platform App UI development
- Test Adapter for MSTest

### Test Settings
Check MI_GUI_WinUI.Tests.runsettings:
- [ ] Correct platform settings
- [ ] Test timeouts configured
- [ ] Coverage settings enabled

### Environment
- [ ] Windows 10/11
- [ ] .NET SDK 8.0 or later
- [ ] PowerShell Core
- [ ] Visual Studio 2022 or VSCode

## Test Data Management

### Adding New Test Data
1. Create data file in appropriate directory
2. Validate against schema
3. Add test cases
4. Update documentation

### Updating Schemas
1. Modify schema file
2. Run validation on all test data
3. Update documentation
4. Update affected tests

## Performance Considerations

### Test Execution
- Run performance tests separately
- Use appropriate test categories
- Consider hardware requirements

### Resource Usage
- Monitor memory usage
- Check GPU availability
- Verify cleanup

## Documentation

### Required Reading
1. README.md - Overview
2. QUICKSTART.md - Getting started
3. VERIFICATION.md - This checklist

### Test Documentation
- Test categories and attributes
- Schema definitions
- Test data format

## Support Files

### Essential Files
- [ ] .runsettings
- [ ] Directory.Build.props
- [ ] Verification scripts
- [ ] Sample data
- [ ] Schema files

### Scripts
- [ ] verify_setup.ps1
- [ ] run_tests.ps1
- [ ] validate_setup.cmd