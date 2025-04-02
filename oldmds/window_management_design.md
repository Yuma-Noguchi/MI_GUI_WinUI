# Window Management and ViewModel Initialization Improvements

## Current Issues

1. **Window Reference Management**
   - Unclear ownership of window references
   - Inconsistent window initialization patterns
   - Direct window manipulation in ViewModels
   - No clear cleanup pattern for window resources

2. **Singleton ViewModel State**
   - ViewModels registered as singletons but contain window-specific state
   - Resource leaks in caches and collections
   - Missing cleanup on navigation/window closure

3. **Navigation and Window Management**
   - Fragmented window management responsibilities
   - Direct window creation bypassing WindowManager
   - Unclear lifecycle between navigation and window state

## Proposed Improvements

### 1. Window Management Service Enhancements

```csharp
public interface IWindowManager
{
    Window MainWindow { get; }
    Window CreateWindow(string title);
    void ShowWindow(Window window);
    void HideWindow(Window window);
    void CloseWindow(Window window);
    void RestoreWindowState(Window window);
    void SaveWindowState(Window window);
}
```

- Add methods for proper window lifecycle management
- Centralize window creation and state management
- Implement proper window cleanup

### 2. ViewModel Lifecycle Management

```csharp
public interface IPageViewModel
{
    void Initialize(Window window);
    Task LoadAsync();
    void Cleanup();
}

public class SelectProfilesViewModel : ObservableObject, IPageViewModel
{
    private readonly IWindowManager _windowManager;
    private WeakReference<Window> _windowReference;
    
    public void Initialize(Window window)
    {
        _windowReference = new WeakReference<Window>(window);
        // Initialize window-specific state
    }
    
    public void Cleanup()
    {
        // Clear caches and release resources
        _previewCache.Clear();
    }
}
```

- Implement clear ViewModel lifecycle methods
- Use WeakReferences to avoid window reference leaks
- Proper cleanup of cached resources

### 3. Navigation Service Integration

```csharp
public interface INavigationService
{
    Task NavigateAsync<TViewModel>(Window window, object parameter = null) 
        where TViewModel : IPageViewModel;
    void GoBack();
}

public class NavigationService : INavigationService
{
    private readonly IWindowManager _windowManager;
    
    public async Task NavigateAsync<TViewModel>(Window window, object parameter = null)
        where TViewModel : IPageViewModel
    {
        var viewModel = Container.GetService<TViewModel>();
        viewModel.Initialize(window);
        // Handle navigation
    }
}
```

- Integrate window management with navigation
- Clear ownership of window lifecycle
- Type-safe ViewModel initialization

## Implementation Plan

1. **Phase 1: Service Refactoring**
   - Update WindowManager with new interface
   - Add lifecycle methods to ViewModels
   - Implement proper cleanup patterns

2. **Phase 2: Navigation Integration**
   - Enhance NavigationService
   - Add window state management
   - Implement ViewModel initialization flow

3. **Phase 3: View Updates**
   - Update pages to use new lifecycle
   - Implement proper cleanup
   - Add error handling

4. **Phase 4: Testing**
   - Add unit tests for window management
   - Test ViewModel lifecycle
   - Verify cleanup patterns

## Migration Strategy

1. **Preparation**
   - Add new interfaces without breaking existing code
   - Create new service implementations alongside existing ones
   - Add lifecycle methods to ViewModels

2. **Gradual Migration**
   - Update one page/feature at a time
   - Verify each migration doesn't break existing features
   - Add comprehensive testing for migrated components

3. **Cleanup**
   - Remove old implementations
   - Update documentation
   - Verify all features work with new implementation

## Benefits

1. **Clear Responsibility**
   - Window management centralized in WindowManager
   - Clear ViewModel lifecycle
   - Type-safe navigation

2. **Resource Management**
   - Proper cleanup of resources
   - No memory leaks from cached items
   - Clear window reference management

3. **Maintainability**
   - Consistent patterns across application
   - Easy to test components
   - Clear separation of concerns

## Risks and Mitigation

1. **Breaking Changes**
   - Implement changes gradually
   - Maintain backward compatibility during migration
   - Comprehensive testing at each step

2. **Performance Impact**
   - Profile memory usage
   - Optimize resource cleanup
   - Cache invalidation strategies

3. **Migration Complexity**
   - Clear documentation
   - Step-by-step migration guide
   - Feature flags for gradual rollout