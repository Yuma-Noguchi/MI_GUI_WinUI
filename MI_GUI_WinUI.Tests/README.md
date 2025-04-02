# MI_GUI_WinUI Test Suite

This directory contains the test suite for the MI_GUI_WinUI application. The test infrastructure is designed to support unit tests, integration tests, and performance tests.

## Project Structure

```
MI_GUI_WinUI.Tests/
├── TestUtils/                # Test utilities and base classes
│   ├── TestBase.cs          # Base class for all tests
│   ├── UnitTestBase.cs      # Base for unit tests
│   ├── IntegrationTestBase.cs # Base for integration tests
│   ├── TestHelpers.cs       # Common test helpers
│   └── TestDataGenerators.cs # Test data generation utilities
├── Services/                 # Service tests
│   ├── ActionServiceTests.cs
│   └── ActionServiceIntegrationTests.cs
├── ViewModels/              # ViewModel tests
│   └── IconStudioViewModelTests.cs
└── Performance/             # Performance tests
    ├── PerformanceTestBase.cs
    └── ImageGenerationPerformanceTests.cs
```

## Test Categories

### Unit Tests
- Inherit from `UnitTestBase`
- Focus on testing individual components in isolation
- Use mock dependencies
- Should be fast and reliable

### Integration Tests
- Inherit from `IntegrationTestBase`
- Test component interactions
- Use real implementations where possible
- May involve file system operations

### Performance Tests
- Inherit from `PerformanceTestBase`
- Measure execution time and resource usage
- Include warmup iterations
- Compare different implementations

## Adding New Tests

1. Choose the appropriate base class:
   ```csharp
   public class MyTests : UnitTestBase
   {
       [TestMethod]
       public async Task MyTest()
       {
           // Test implementation
       }
   }
   ```

2. Use test utilities:
   ```csharp
   // Generate test data
   var testAction = TestDataGenerators.CreateAction();

   // Use mock services
   MockActionService.Setup(/*...*/);
   ```

3. Follow naming conventions:
   - Test classes: `{Component}Tests.cs`
   - Test methods: `{Scenario}_{ExpectedResult}`

## Test Data Management

Use `TestDataGenerators` for consistent test data:
```csharp
// Create test actions
var action = TestDataGenerators.CreateAction("Test Action");

// Create test images
var imageData = TestDataGenerators.CreateTestImage();

// Generate test prompts
var prompts = TestDataGenerators.CreateTestPrompts();
```

## Performance Testing

Performance tests should:
1. Include warmup iterations
2. Run multiple times for reliable results
3. Use realistic data sizes
4. Test both normal and edge cases

Example:
```csharp
[TestMethod]
public async Task PerformanceTest()
{
    var testName = "MyOperation";
    await RunPerformanceTest(testName, async () =>
    {
        // Test operation
    });
    AssertPerformance(testName, maxAverageMs, maxP95Ms);
}
```

## Test Coverage Targets

- Services: 95%
- ViewModels: 90%
- Models: 85%
- UI Components: 60%

## Running Tests

### Visual Studio
1. Open Test Explorer
2. Select test category or individual tests
3. Run selected tests

### Command Line
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=UnitTest"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Performance"
```

### CI/CD Pipeline
Tests are automatically run in the CI pipeline with:
- Unit tests on every PR
- Integration tests on merge to main
- Performance tests nightly

## Best Practices

1. **Test Independence**
   - Tests should not depend on each other
   - Clean up resources after each test
   - Use fresh test data for each test

2. **Mock Usage**
   - Mock external dependencies
   - Use strict mocks when possible
   - Verify mock interactions

3. **Assertions**
   - Make specific assertions
   - Include meaningful messages
   - Test both positive and negative cases

4. **Performance Testing**
   - Include warmup iterations
   - Test with realistic data sizes
   - Log detailed metrics
   - Compare against baseline

5. **Test Data**
   - Use `TestDataGenerators`
   - Avoid hardcoded test data
   - Clean up test files

6. **Error Handling**
   - Test error conditions
   - Verify error messages
   - Test recovery scenarios

## Troubleshooting

Common issues and solutions:

1. **Test Failures in CI but not Locally**
   - Check file paths (use Path.Combine)
   - Verify cleanup in TestCleanup
   - Check for machine-specific configurations

2. **Slow Tests**
   - Use appropriate test category
   - Minimize file I/O in unit tests
   - Profile test execution

3. **Flaky Tests**
   - Add retry logic where appropriate
   - Check for race conditions
   - Verify cleanup between tests

## Contributing

1. Follow existing patterns
2. Update documentation
3. Include both positive and negative tests
4. Verify performance impact
5. Run full test suite before submitting PR