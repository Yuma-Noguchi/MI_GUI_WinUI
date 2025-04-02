# Test Data Samples

This directory contains sample test data files used for validating the test infrastructure and providing examples for test development.

## Files

### sample_profile.json
Sample profile configuration that demonstrates:
- Basic profile structure
- GUI elements configuration
- Pose elements configuration
- Settings and metadata

Usage:
```csharp
[TestMethod]
[DataDependentTest("Profiles")]
public async Task TestProfileLoading()
{
    var profilePath = GetSampleFilePath("sample_profile.json");
    // Test implementation
}
```

### sample_action.json
Sample action definitions that demonstrate:
- Mouse actions
- Keyboard actions
- Action sequences
- Pose-triggered actions
- Conditional actions

Usage:
```csharp
[TestMethod]
[DataDependentTest("Actions")]
public async Task TestActionLoading()
{
    var actionPath = GetSampleFilePath("sample_action.json");
    // Test implementation
}
```

## Guidelines for Test Data

1. Sample files should:
   - Be minimal but complete
   - Cover common use cases
   - Include comments/documentation
   - Use consistent formatting
   - Maintain backward compatibility

2. When updating samples:
   - Document changes in this README
   - Update corresponding tests
   - Validate against schemas
   - Test with validation script

3. Data Structure:
   ```
   TestData/
   ├── Samples/            # Base sample files
   │   ├── README.md
   │   ├── sample_profile.json
   │   └── sample_action.json
   ├── Profiles/          # Test-specific profiles
   ├── Actions/           # Test-specific actions
   ├── Config/            # Test configurations
   └── Prompts/           # Test prompts
   ```

## Validation

Run the validation script to verify sample data:
```bash
.\validate_setup.cmd
```

The script will:
1. Check if sample files exist
2. Validate file format
3. Copy to test directories if needed
4. Run verification tests

## Schema Validation

Sample files should validate against schemas in `Schemas/`:
- Profile Schema: `profile-schema.json`
- Action Schema: `action-schema.json`

## Customizing Test Data

1. Copy sample files to appropriate test directories
2. Modify as needed for specific tests
3. Update or add test cases
4. Document test data dependencies

Example:
```csharp
public class MyTests : UnitTestBase
{
    private string _testProfilePath;
    private string _testActionPath;

    [TestInitialize]
    public override async Task InitializeTest()
    {
        await base.InitializeTest();
        
        // Copy and customize test data
        _testProfilePath = await CopyAndCustomizeSampleProfile("custom_profile.json");
        _testActionPath = await CopyAndCustomizeSampleAction("custom_action.json");
    }

    [TestMethod]
    public async Task TestWithCustomData()
    {
        // Use customized test data
        var profile = await LoadProfile(_testProfilePath);
        var action = await LoadAction(_testActionPath);
        
        // Test implementation
    }
}
```

## Maintaining Test Data

1. Keep samples up to date with application changes
2. Regularly validate samples
3. Remove obsolete data
4. Document breaking changes
5. Version control test data

## Troubleshooting

1. Missing sample files:
   - Run validation script
   - Check file permissions
   - Verify source control

2. Invalid data format:
   - Validate against schema
   - Check JSON formatting
   - Review recent changes

3. Test failures:
   - Compare with samples
   - Check customizations
   - Verify dependencies