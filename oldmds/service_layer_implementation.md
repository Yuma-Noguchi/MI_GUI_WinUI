# Service Layer Implementation Plan

## 1. Create Service Interfaces

### IActionService Interface
```csharp
public interface IActionService
{
    Task<List<ActionData>> LoadActionsAsync();
    Task SaveActionAsync(ActionData action);
    Task DeleteActionAsync(string actionId);
    Task<ActionData> GetActionByIdAsync(string id);
    Task<ActionData> GetActionByNameAsync(string name);
    void ClearCache();
}
```

### IMotionInputService Interface
```csharp
public interface IMotionInputService
{
    Task<bool> StartProfileAsync(string profileName);
    Task<bool> StopProfileAsync(string profileName);
    Task<bool> LaunchAsync();
    Task<List<string>> GetAvailableProfilesAsync();
}
```

### ILoggingService Interface
```csharp
public interface ILoggingService
{
    void LogInformation(string message);
    void LogError(Exception ex, string message);
    void LogWarning(string message);
    void LogDebug(string message);
}
```

### IStableDiffusionService Interface
```csharp
public interface IStableDiffusionService
{
    Task<byte[]> GenerateImageAsync(string prompt);
    Task<bool> InitializeAsync();
    void Cleanup();
}
```

## 2. Update Existing Services

### ActionService Updates
1. Implement IActionService interface
2. Remove direct file system access from constructor
3. Add configuration injection for file paths
4. Add proper exception handling

### Example Implementation:
```csharp
public class ActionService : IActionService
{
    private readonly ILogger<ActionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _actionsFilePath;
    private Dictionary<string, ActionData> _actions = new();

    public ActionService(
        ILogger<ActionService> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _actionsFilePath = _configuration["Paths:ActionsFile"];
        EnsureActionsFolderExists();
    }
    // ... implementation of interface methods
}
```

## 3. Dependency Injection Configuration

### Update App.xaml.cs
```csharp
public partial class App : Application
{
    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Services
        services.AddSingleton<IActionService, ActionService>();
        services.AddSingleton<IMotionInputService, MotionInputService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IStableDiffusionService, StableDiffusionService>();

        // ViewModels (convert to transient)
        services.AddTransient<SelectProfilesViewModel>();
        services.AddTransient<ActionStudioViewModel>();
    }
}
```

## 4. Unit Testing Setup

### Test Base Classes
```csharp
public abstract class ServiceTestBase
{
    protected Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected Mock<IConfiguration> CreateConfiguration()
    {
        var configMock = new Mock<IConfiguration>();
        // Add common configuration setup
        return configMock;
    }
}
```

### Example Test Class
```csharp
public class ActionServiceTests : ServiceTestBase
{
    private readonly Mock<ILogger<ActionService>> _logger;
    private readonly Mock<IConfiguration> _config;
    private readonly IActionService _service;

    public ActionServiceTests()
    {
        _logger = CreateLogger<ActionService>();
        _config = CreateConfiguration();
        _service = new ActionService(_logger.Object, _config.Object);
    }

    [Fact]
    public async Task LoadActionsAsync_WhenFileExists_ReturnsActions()
    {
        // Arrange
        // ... test setup

        // Act
        var result = await _service.LoadActionsAsync();

        // Assert
        Assert.NotNull(result);
    }
}
```

## Implementation Steps

1. **Phase 1: Interface Creation**
   - Create Interfaces folder in Services directory
   - Create all interface files
   - Document interface contracts

2. **Phase 2: Service Updates**
   - Update each service to implement its interface
   - Inject proper dependencies
   - Remove static methods
   - Add error handling

3. **Phase 3: DI Configuration**
   - Update App.xaml.cs with service registration
   - Add configuration file support
   - Convert appropriate services to transient

4. **Phase 4: Testing**
   - Create test project structure
   - Implement test base classes
   - Add service tests
   - Validate DI configuration

## Success Criteria
- All services implement interfaces
- No static methods remain
- Proper DI configuration
- Unit tests for core functionality
- Error handling implemented
- Configuration externalized

## Migration Notes
- Update service references in ViewModels
- Update service resolution in Pages
- Test each service after migration
- Validate error handling
- Check service lifetime management

## Testing Strategy
- Unit tests for each service
- Integration tests for service interactions
- Mock external dependencies
- Test error conditions
- Validate configuration handling