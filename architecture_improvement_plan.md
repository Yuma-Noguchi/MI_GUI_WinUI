# Architecture Improvement Plan

## Overview
This document outlines the architectural improvements for the MI_GUI_WinUI application to enhance maintainability, testability, and scalability while maintaining MVVM best practices.

## 1. Service Layer Improvements (Priority: High)

### 1.1 Dependency Injection Implementation
**Current State:**
- Some services use static methods
- Limited use of interface-based design
- Manual dependency management

**Improvement Plan:**
1. Create interfaces for all services:
```csharp
public interface IMotionInputService
{
    Task<bool> Start(string profileName);
    Task<bool> ChangeMode(string mode);
    Task<bool> Launch();
}

public interface IActionService
{
    Task<List<ActionData>> LoadActionsAsync();
    Task SaveActionAsync(ActionData action);
    Task DeleteActionAsync(string actionId);
}
```

2. Implement Microsoft.Extensions.DependencyInjection:
```csharp
services.AddSingleton<IMotionInputService, MotionInputService>();
services.AddSingleton<IActionService, ActionService>();
services.AddSingleton<IWindowManager, WindowManager>();
```

3. Update service constructors to accept dependencies:
```csharp
public class MotionInputService : IMotionInputService
{
    private readonly ILoggingService _logger;
    private readonly IConfigurationService _config;
    
    public MotionInputService(ILoggingService logger, IConfigurationService config)
    {
        _logger = logger;
        _config = config;
    }
}
```


## 4. Testing Infrastructure (Priority: Medium)

### 4.1 Unit Testing Setup
1. Create test projects:
- MI_GUI_WinUI.Tests.Unit
- MI_GUI_WinUI.Tests.Integration

2. Implement test base classes:
```csharp
public abstract class ViewModelTestBase
{
    protected Mock<INavigationService> NavigationService;
    protected Mock<ILoggingService> LoggingService;
    
    protected ViewModelTestBase()
    {
        NavigationService = new Mock<INavigationService>();
        LoggingService = new Mock<ILoggingService>();
    }
}
```

### 4.2 Mocking Infrastructure
1. Create mock repositories for testing:
```csharp
public class InMemoryProfileRepository : IProfileRepository
{
    private readonly Dictionary<string, Profile> _profiles = new();

    public Task<Profile> GetByNameAsync(string name)
    {
        return Task.FromResult(_profiles.GetValueOrDefault(name));
    }
}
```

## Implementation Timeline

### Phase 1 (Weeks 1-2)
- Set up dependency injection infrastructure
- Create interfaces for all services
- Implement repository pattern for Profile and Action services

### Phase 2 (Weeks 3-4)
- Refactor SelectProfilesViewModel
- Implement Unit of Work pattern
- Add FluentValidation

### Phase 3 (Weeks 5-6)
- Set up testing infrastructure
- Create unit tests for core functionality
- Implement integration tests

## Success Criteria
1. All services are interface-based and properly injected
2. ViewModels are more focused and maintainable
3. Data operations are consistent and transactional
4. Test coverage is at least 80% for core functionality
5. No direct file system access in services
6. Validation is consistent across the application

## Risks and Mitigations
1. **Risk**: Breaking changes during refactoring
   **Mitigation**: Implement changes gradually with comprehensive testing

2. **Risk**: Performance impact from additional abstraction layers
   **Mitigation**: Profile and optimize critical paths

3. **Risk**: Learning curve for new patterns
   **Mitigation**: Provide documentation and team training sessions