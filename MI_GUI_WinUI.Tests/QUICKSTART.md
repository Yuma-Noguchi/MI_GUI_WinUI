# MI_GUI_WinUI Test Infrastructure Quick Start Guide

## Initial Setup

1. Verify test infrastructure:
```powershell
.\verify_setup.ps1
```

2. Run all tests:
```powershell
.\run_tests.ps1
```

## Test Categories

Tests are organized into the following categories:

- `[SmokeTest]`: Basic functionality tests that should run quickly
- `[IntegrationTest]`: Tests that verify component integration
- `[PerformanceTest]`: Performance benchmarks and measurements
- `[UITest]`: Tests for UI components
- `[LongRunningTest]`: Tests that take significant time to run
- `[DataDependentTest]`: Tests that require specific test data
- `[ModifiesData]`: Tests that modify test data
- `[RequiresPlatform]`: Tests that need specific platform features
- `[IsolatedTest]`: Tests that must run in isolation

## Running Specific Test Categories

```powershell
# Run smoke tests only
dotnet test --filter "TestCategory=Smoke"

# Run all tests except long-running
dotnet test --filter "TestCategory!=LongRunning"
```

## Adding New Tests

1. Choose appropriate base class:
```csharp
// For unit tests
public class MyTests : UnitTestBase

// For integration tests
public class MyIntegrationTests : IntegrationTestBase

// For performance tests
public class MyPerformanceTests : PerformanceTestBase
```

2. Add test categories:
```csharp
[TestClass]
public class MyTests : UnitTestBase
{
    [TestMethod]
    [SmokeTest]
    public void MyQuickTest()
    {
        // Test implementation
    }

    [TestMethod]
    [IntegrationTest]
    public async Task MyIntegrationTest()
    {
        // Integration test implementation
    }
}
```

3. Use test utilities:
```csharp
// Generate test data
var action = TestDataGenerators.CreateAction();

// Use test helpers
await VerifyException<InvalidOperationException>(async () => 
{
    // Test code that should throw
});
```

## Test Data Management

Test data is organized in the following directories:
```
TestData/
├── Profiles/      # Profile test data
├── Actions/       # Action definitions
├── Config/        # Test configurations
└── Prompts/       # Image generation prompts
```

Use the appropriate test attributes when your tests depend on this data:
```csharp
[TestMethod]
[DataDependentTest("Profiles")]
public void TestWithProfileData()
{
    // Test implementation
}
```

## Performance Testing

1. Basic performance test:
```csharp
[TestMethod]
[PerformanceTest]
public async Task VerifyImageGenerationPerformance()
{
    await RunPerformanceTest("ImageGeneration", async () =>
    {
        // Test implementation
    });

    AssertPerformance("ImageGeneration", 
        maxAverageMs: 5000, 
        maxP95Ms: 7000);
}
```

2. Memory usage test:
```csharp
[TestMethod]
[PerformanceTest]
public async Task VerifyMemoryUsage()
{
    var (peakMemory, elapsed) = await MeasureMemoryUsage(async () =>
    {
        // Test implementation
    });

    AssertMemoryUsage(peakMemory, maxBytes: 1024 * 1024 * 1024); // 1GB
}
```

## Continuous Integration

Tests run automatically on:
- Pull requests (smoke tests)
- Main branch merges (all tests)
- Nightly builds (including performance tests)

Configure test execution in `azure-pipelines.yml`.

## Best Practices

1. Always use appropriate test categories
2. Include both positive and negative test cases
3. Clean up test resources in TestCleanup
4. Use test data generators for consistency
5. Document performance test thresholds
6. Handle async operations properly

## Troubleshooting

1. Test discovery issues:
   - Ensure test class has `[TestClass]` attribute
   - Ensure test methods have `[TestMethod]` attribute
   - Verify test project references

2. Performance test failures:
   - Check if running on correct hardware
   - Verify no background processes affecting results
   - Check test data size matches expectations

3. Data-dependent test failures:
   - Verify test data files exist
   - Check file permissions
   - Ensure test data is in correct format

4. UI test issues:
   - Ensure running on correct Windows version
   - Check WinAppSDK dependencies
   - Verify UI thread handling

## Getting Help

1. Check test output in TestResults directory
2. Review test logs in Output window
3. Use `--verbosity detailed` for more information
4. Refer to test implementation in case of confusion