# MI_GUI_WinUI Test Suite

This directory contains the comprehensive test suite for the MI_GUI_WinUI application. The test infrastructure is designed to support unit tests, integration tests, and performance tests, following industry-standard testing patterns and principles.

## Design Patterns in Testing

The test architecture implements several design patterns to ensure maintainability, reliability, and comprehensive coverage:

### 1. Base Test Class Hierarchy

A hierarchical approach to test classes that promotes code reuse and consistent setup:

```csharp
TestBase (common foundation)
├── UnitTestBase (isolated component testing)
├── IntegrationTestBase (cross-component validation)
└── PerformanceTestBase (performance benchmarking)
```

This pattern allows for:
- Consistent test initialization and cleanup
- Shared mock setup logic
- Common assertion utilities
- Standardized test lifecycle management

### 2. Arrange-Act-Assert (AAA) Pattern

All tests follow the AAA pattern for clarity and consistency:

```csharp
[TestMethod]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange - Set up test conditions
    var testData = TestDataGenerators.CreateTestData();
    MockService.Setup(s => s.Method()).ReturnsAsync(expectedResult);
    
    // Act - Perform the operation under test
    var result = await _sut.MethodAsync(testData);
    
    // Assert - Verify the expected outcome
    Assert.AreEqual(expectedResult, result);
    MockService.Verify(s => s.Method(), Times.Once);
}
```

### 3. Mock Object Pattern

The test suite uses Moq to create mock implementations of interfaces:

```csharp
// Mock creation and setup
private Mock<INavigationService> _mockNavigation;
private Mock<IProfileService> _mockProfileService;

// Setup in test initialization
_mockNavigation = new Mock<INavigationService>();
_mockNavigation.Setup(n => n.Navigate<TPage, TViewModel>(It.IsAny<Window>(), It.IsAny<object>()))
    .Returns(true);
```

This enables:
- Isolation of the System Under Test (SUT)
- Control over dependency behavior
- Verification of interactions
- Simulation of edge cases and error conditions

### 4. Test Data Builder Pattern

The TestDataGenerators class implements a builder pattern for creating test data:

```csharp
public static class TestDataGenerators
{
    public static Profile CreateProfile(string name = "Test Profile")
    {
        return new Profile
        {
            Name = name,
            Description = "Test description",
            IsEnabled = true,
            GlobalConfig = new Dictionary<string, string>(),
            GuiElements = new List<GuiElement>(),
            Poses = new List<PoseGuiElement>(),
            SpeechCommands = new Dictionary<string, SpeechCommand>()
        };
    }
    
    public static ActionData CreateAction(string name = "Test Action")
    {
        // Implementation
    }
    
    // Additional factory methods
}
```

### 5. Test Fixture Pattern

Test fixtures provide reusable test setup and resources:

```csharp
public class TestFixture : IDisposable
{
    public string TempDirectory { get; }
    public List<Profile> SampleProfiles { get; }
    
    public TestFixture()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(TempDirectory);
        SampleProfiles = CreateSampleProfiles();
    }
    
    public void Dispose()
    {
        if (Directory.Exists(TempDirectory))
            Directory.Delete(TempDirectory, true);
    }
    
    private List<Profile> CreateSampleProfiles()
    {
        // Create test data
    }
}
```

This pattern allows:
- Shared resources across tests
- Proper resource cleanup
- Consistent test data
- Reduced test setup duplication

## Testing Principles

The test suite adheres to the following testing principles:

### 1. Test Independence

Each test runs independently and doesn't rely on the state from other tests:
- Tests create their own test data
- Tests clean up resources after execution
- No shared state between tests
- Deterministic test execution

### 2. Single Assertion Concept

Each test focuses on verifying a single behavior or concept:
- Clear test purpose
- Specific failure messages
- Easier maintenance
- Better failure diagnosis

### 3. Test Naming Convention

Tests follow a descriptive naming pattern:
```
MethodName_Scenario_ExpectedResult
```

Examples:
- `SaveProfile_WithValidData_ReturnsSuccess`
- `LoadProfile_WhenFileNotFound_ThrowsException`
- `GenerateImage_WithLongPrompt_CompletesSucessfully`

### 4. Comprehensive Coverage Strategy

The test suite aims for high coverage across different dimensions:
- **Code Coverage**: Lines, branches, and conditions
- **Functional Coverage**: Features and use cases
- **Edge Cases**: Boundary values and error conditions
- **Performance Scenarios**: Resource usage and timing

### 5. Test Parameterization

Parameterized tests to validate multiple scenarios with shared logic:

```csharp
[DataTestMethod]
[DataRow("", "Profile name cannot be empty")]
[DataRow("Invalid/Name", "Profile name contains invalid characters")]
[DataRow("A name that is way too long for a profile", "Profile name is too long")]
public void ValidateProfileName_WithInvalidName_ReturnsError(string name, string expectedError)
{
    // Arrange
    var profile = TestDataGenerators.CreateProfile();
    profile.Name = name;
    
    // Act
    var result = _validator.ValidateProfileName(profile);
    
    // Assert
    Assert.IsFalse(result.IsValid);
    Assert.AreEqual(expectedError, result.ErrorMessage);
}
```

## Project Structure

```
MI_GUI_WinUI.Tests/
├── Infrastructure/                # Test framework components
│   ├── TestBase.cs              # Base test class
│   ├── UnitTestBase.cs          # Base for unit tests
│   ├── IntegrationTestBase.cs   # Base for integration tests
│   └── PerformanceTestBase.cs   # Base for performance tests
├── TestUtils/                    # Shared test utilities
│   ├── TestDataGenerators.cs    # Test data factory
│   ├── MockHelpers.cs           # Mock configuration utilities
│   └── TestFixture.cs           # Reusable test fixtures
├── Services/                     # Service tests
│   ├── ActionServiceTests.cs
│   ├── ProfileServiceTests.cs
│   ├── NavigationServiceTests.cs
│   └── StableDiffusionServiceTests.cs
├── ViewModels/                   # ViewModel tests
│   ├── ActionStudioViewModelTests.cs
│   ├── IconStudioViewModelTests.cs
│   ├── ProfileEditorViewModelTests.cs
│   └── SelectProfilesViewModelTests.cs
└── Performance/                  # Performance tests
    ├── StableDiffusionPerformanceTests.cs
    ├── ProfileLoadingPerformanceTests.cs
    └── PerformanceTracker.cs
```

## Test Categories

### Unit Tests
- Inherit from `UnitTestBase`
- Focus on testing individual components in isolation
- Use mock dependencies
- Should be fast and reliable
- Cover both success and failure paths

Example:
```csharp
[TestClass]
public class ProfileServiceTests : UnitTestBase
{
    private Mock<ILogger<ProfileService>> _mockLogger;
    private Mock<IFileSystem> _mockFileSystem;
    private ProfileService _profileService;

    [TestInitialize]
    public override async Task InitializeTest()
    {
        await base.InitializeTest();
        
        _mockLogger = new Mock<ILogger<ProfileService>>();
        _mockFileSystem = new Mock<IFileSystem>();
        
        _profileService = new ProfileService(
            _mockLogger.Object,
            _mockFileSystem.Object);
    }

    [TestMethod]
    public async Task GetProfileAsync_WhenProfileExists_ReturnsProfile()
    {
        // Arrange
        var profileId = "test-profile-id";
        var expectedProfile = TestDataGenerators.CreateProfile();
        
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(JsonConvert.SerializeObject(expectedProfile));
        
        // Act
        var result = await _profileService.GetProfileAsync(profileId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedProfile.Name, result.Name);
        // Additional assertions
    }
}
```

### Integration Tests
- Inherit from `IntegrationTestBase`
- Test component interactions
- Use real implementations where possible
- May involve file system operations
- Focus on cross-component behavior

Example:
```csharp
[TestClass]
public class ProfileIntegrationTests : IntegrationTestBase
{
    private string _testDirectory;
    private IProfileService _profileService;
    private IActionService _actionService;

    [TestInitialize]
    public override async Task InitializeTest()
    {
        await base.InitializeTest();
        
        _testDirectory = Path.Combine(Path.GetTempPath(), $"MI_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        var serviceProvider = CreateServiceProvider(_testDirectory);
        _profileService = serviceProvider.GetRequiredService<IProfileService>();
        _actionService = serviceProvider.GetRequiredService<IActionService>();
    }
    
    [TestCleanup]
    public override async Task CleanupTest()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
            
        await base.CleanupTest();
    }
    
    [TestMethod]
    public async Task ProfileWithActions_SaveAndLoad_PreservesActionAssignments()
    {
        // Arrange
        var profile = TestDataGenerators.CreateProfile();
        var action = TestDataGenerators.CreateAction();
        
        // Save action first
        await _actionService.SaveActionAsync(action);
        
        // Add action to profile
        profile.GuiElements.Add(new GuiElement
        {
            File = "button.png",
            Position = new List<int> { 100, 100 },
            Action = new ActionConfig { Id = action.Id }
        });
        
        // Act
        await _profileService.SaveProfileAsync(profile);
        var loadedProfile = await _profileService.GetProfileAsync(profile.Id);
        
        // Assert
        Assert.IsNotNull(loadedProfile);
        Assert.AreEqual(1, loadedProfile.GuiElements.Count);
        Assert.AreEqual(action.Id, loadedProfile.GuiElements[0].Action.Id);
    }
}
```

### Performance Tests
- Inherit from `PerformanceTestBase`
- Measure execution time and resource usage
- Include warmup iterations
- Compare different implementations
- Set performance thresholds

Example:
```csharp
[TestClass]
public class StableDiffusionPerformanceTests : PerformanceTestBase
{
    private IStableDiffusionService _service;
    
    [TestInitialize]
    public override async Task InitializeTest()
    {
        await base.InitializeTest();
        _service = CreateStableDiffusionService();
        
        // Warmup
        await _service.GenerateImageAsync("test prompt", CancellationToken.None);
    }
    
    [TestMethod]
    public async Task GenerateImage_Performance_MeetsThreshold()
    {
        // Arrange
        var prompt = "a photo of a cat";
        var iterations = 3;
        
        // Act
        StartMeasurement();
        
        for (int i = 0; i < iterations; i++)
        {
            await _service.GenerateImageAsync(prompt, CancellationToken.None);
        }
        
        var metrics = StopMeasurement();
        
        // Assert
        AssertPerformance(
            metrics.AverageExecutionTime,
            maxAcceptableMs: 10000,
            $"Image generation took too long: {metrics.AverageExecutionTime}ms");
    }
}
```

## Command to Run Tests
```bash
# Build the project
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" MI_GUI_WinUI.Tests\MI_GUI_WinUI.Tests.csproj

# Run all tests
dotnet test --no-build MI_GUI_WinUI.Tests\MI_GUI_WinUI.Tests.csproj --configuration Release -p:Platform=x64 --logger "trx;LogFileName=unit-tests.trx" 
```

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

## Test-Driven Development

The project follows a test-driven development approach:

1. **Write a failing test** that defines the expected behavior
2. **Implement the minimum code** required to pass the test
3. **Refactor the code** while keeping tests passing
4. **Repeat** for each new feature or behavior

This approach ensures:
- Code is testable by design
- Requirements are verified through tests
- Regression protection
- Clean, focused implementations