# Service Layer Implementation Guide

## Overview
This guide outlines the steps required to implement the new service layer architecture across the application. The main goals are to:
1. Ensure consistent dependency injection
2. Implement interface-based services
3. Add proper configuration support
4. Improve error handling and logging

## Implemented Services

### 1. Action Service
- Interface: `IActionService`
- Implementation: `ActionService`
- Configuration Keys:
  ```json
  {
    "Paths": {
      "ActionsFile": "path/to/actions.json"
    }
  }
  ```

### 2. Motion Input Service
- Interface: `IMotionInputService`
- Implementation: `MotionInputService`
- Configuration Keys:
  ```json
  {
    "Paths": {
      "MotionInputConfig": "path/to/config.json",
      "MotionInputExe": "path/to/MotionInput.exe"
    }
  }
  ```

### 3. Logging Service
- Interface: `ILoggingService`
- Implementation: `LoggingService`
- Configuration Keys:
  ```json
  {
    "Logging": {
      "Folder": "Logs",
      "FileName": "MotionInput_Configuration.log",
      "IncludeDebug": false
    }
  }
  ```

### 4. Navigation Service
- Interface: `INavigationService`
- Implementation: `NavigationService`
- Dependencies:
  - ILogger<NavigationService>
  - ILoggingService

### 5. Stable Diffusion Service
- Interface: `IStableDiffusionService`
- Implementation: `StableDiffusionService`
- Configuration Keys:
  ```json
  {
    "StableDiffusion": {
      "ModelsPath": "Onnx/fp16",
      "OutputPath": "Generated",
      "InferenceSteps": 75,
      "GuidanceScale": 8.5,
      "DeviceId": 1
    }
  }
  ```

### 6. Window Manager Service
- Interface: `IWindowManager`
- Implementation: `WindowManager`
- Configuration Keys:
  ```json
  {
    "Windows": {
      "DefaultWidth": 800,
      "DefaultHeight": 600
    }
  }
  ```

## Required Dependencies

### NuGet Packages
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="7.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

## Implementation Steps

### 1. Add NuGet Dependencies
1. Add the required NuGet packages to your project
2. Ensure all packages are compatible with your target framework
3. Restore NuGet packages

### 2. Configuration Setup
1. Create `appsettings.json` in project root:
```json
{
  "Paths": {
    "ActionsFile": "MotionInput/data/actions/actions.json",
    "MotionInputConfig": "MotionInput/data/config.json",
    "MotionInputExe": "MotionInput/MotionInput.exe"
  },
  "Logging": {
    "Folder": "Logs",
    "FileName": "MotionInput_Configuration.log",
    "IncludeDebug": false
  },
  "StableDiffusion": {
    "ModelsPath": "Onnx/fp16",
    "OutputPath": "Generated",
    "InferenceSteps": 75,
    "GuidanceScale": 8.5,
    "DeviceId": 1
  },
  "Windows": {
    "DefaultWidth": 800,
    "DefaultHeight": 600
  }
}
```

2. Update `App.xaml.cs` to load configuration:
```csharp
public partial class App : Application
{
    private void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Register services
        services.AddSingleton<IActionService, ActionService>();
        services.AddSingleton<IMotionInputService, MotionInputService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IStableDiffusionService, StableDiffusionService>();
        services.AddSingleton<IWindowManager, WindowManager>();

        // Register transient ViewModels
        services.AddTransient<SelectProfilesViewModel>();
        services.AddTransient<ActionStudioViewModel>();
    }
}
```

### 2. ViewModels Update
Update ViewModels to use service interfaces:

```csharp
public class SelectProfilesViewModel : ViewModelBase
{
    private readonly IActionService _actionService;
    private readonly IMotionInputService _motionInputService;
    private readonly ILoggingService _loggingService;

    public SelectProfilesViewModel(
        IActionService actionService,
        IMotionInputService motionInputService,
        ILoggingService loggingService)
    {
        _actionService = actionService;
        _motionInputService = motionInputService;
        _loggingService = loggingService;
    }
}
```

### 3. Error Handling Pattern
Implement consistent error handling across ViewModels:

```csharp
private async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string errorMessage)
{
    try
    {
        await action();
    }
    catch (Exception ex)
    {
        _loggingService.LogError(ex, errorMessage);
        ErrorMessage = errorMessage;
    }
}
```

### 4. Migration Process
1. Identify all ViewModels using old service implementations
2. Update constructor dependencies to use interfaces
3. Replace direct service usage with interface methods
4. Update error handling to use new logging pattern
5. Test each ViewModel after migration

### 5. Testing
1. Create unit tests for each service:
```csharp
public class ActionServiceTests
{
    private readonly Mock<ILogger<ActionService>> _logger;
    private readonly Mock<IConfiguration> _config;
    private readonly IActionService _service;

    public ActionServiceTests()
    {
        _logger = new Mock<ILogger<ActionService>>();
        _config = new Mock<IConfiguration>();
        _service = new ActionService(_logger.Object, _config.Object);
    }

    [Fact]
    public async Task SaveActionAsync_ValidAction_SavesSuccessfully()
    {
        // Arrange
        var action = new ActionData { /* ... */ };

        // Act
        await _service.SaveActionAsync(action);

        // Assert
        // Verify action was saved
    }
}
```

## Verification Checklist
- [ ] All services implement their interfaces
- [ ] Configuration is properly loaded
- [ ] ViewModels use interface injection
- [ ] Error handling is consistent
- [ ] Unit tests pass
- [ ] No direct file system access in ViewModels
- [ ] Logging is properly implemented
- [ ] Services are registered with correct lifetimes

## Common Issues and Solutions
1. **Configuration Missing**
   - Ensure appsettings.json is copied to output directory
   - Verify configuration section names match code

2. **DI Resolution Errors**
   - Check service registration in App.xaml.cs
   - Verify constructor parameter order
   - Check for circular dependencies

3. **File Access Errors**
   - Use configuration paths instead of hardcoded values
   - Ensure proper error handling for file operations
   - Log file access issues with full paths

## Support
For questions about the implementation:
1. Check the service interfaces for available methods
2. Review the test cases for usage examples
3. Consult the error handling pattern for specific scenarios