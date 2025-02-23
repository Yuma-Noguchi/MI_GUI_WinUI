# MotionInput GUI Architecture Documentation

## Table of Contents
1. [Application Structure](#application-structure)
2. [Service Architecture](#service-architecture)
3. [Window Management](#window-management)
4. [Navigation System](#navigation-system)
5. [ViewModel Pattern](#viewmodel-pattern)
6. [Page Implementation Guide](#page-implementation-guide)
7. [Best Practices](#best-practices)

## Application Structure

### Project Organization
```
MI_GUI_WinUI/
├── App.xaml/App.xaml.cs        # Application entry point and DI configuration
├── Services/                    # Core services
├── ViewModels/                 # MVVM ViewModels
├── Pages/                      # UI Pages
├── Models/                     # Business logic and data models
├── Controls/                   # Reusable UI components
└── Converters/                 # Value converters for XAML binding
```

### Key Components
- **App.xaml.cs**: Application bootstrapping and dependency injection configuration
- **WindowManager**: Centralized window lifecycle management
- **NavigationService**: Page navigation and state management
- **ViewModels**: Business logic and UI state management
- **Pages**: UI implementation with XAML

## Service Architecture

### Dependency Injection
```csharp
// App.xaml.cs
public App()
{
    var services = new ServiceCollection();
    
    // Register core services
    services.AddSingleton<WindowManager>(_windowManager);
    services.AddSingleton<INavigationService, NavigationService>();
    services.AddSingleton<ProfileService>();
    
    // Register ViewModels
    services.AddSingleton<MainWindowViewModel>();
    services.AddSingleton<SelectProfilesViewModel>();
    
    // Build provider
    _serviceProvider = services.BuildServiceProvider();
    Ioc.Default.ConfigureServices(_serviceProvider);
}
```

### Service Lifetime Guidelines
- **Singleton**: Use for services that maintain application-wide state
  - WindowManager
  - NavigationService
  - ViewModels (if state should persist)
- **Transient**: Use for stateless services or per-request instances
  - Pages
  - Utility services

## Window Management

### WindowManager Service
```csharp
public class WindowManager
{
    private readonly Dictionary<Guid, Window> _windows;
    private Window _mainWindow;
    
    // Window lifecycle methods
    public void InitializeMainWindow()
    public void ActivateWindow(Window window)
    public void SaveWindowState(Window window)
    public void RestoreWindowState(Window window)
}
```

### Window State Management
- Each window has a unique identifier
- Window position and size are persisted
- Proper cleanup on window closure
- Main window management with application lifecycle integration

## Navigation System

### Navigation Service
```csharp
public interface INavigationService
{
    bool Navigate<TPage, TViewModel>(object? parameter = null) 
        where TPage : Page
        where TViewModel : class;
    bool GoBack();
    event EventHandler<string> NavigationChanged;
}
```

### Navigation Pattern
```csharp
// Example navigation with ViewModel
_navigationService.Navigate<SelectProfilesPage, SelectProfilesViewModel>();

// Navigation with parameters
_navigationService.Navigate<DetailPage, DetailViewModel>(itemId);
```

## ViewModel Pattern

### ViewModel Base Structure
```csharp
public partial class ViewModelBase : ObservableObject
{
    private readonly ILogger _logger;
    private readonly INavigationService _navigationService;
    private WeakReference<Window>? _windowReference;

    protected ViewModelBase(
        ILogger logger,
        INavigationService navigationService)
    {
        _logger = logger;
        _navigationService = navigationService;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual void Cleanup() { }
}
```

### ViewModel Initialization
```csharp
public sealed partial class SelectProfilesPage : Page
{
    private void SetViewModel(SelectProfilesViewModel viewModel)
    {
        _viewModel = viewModel;
        var windowManager = Ioc.Default.GetRequiredService<WindowManager>();
        _viewModel.Window = windowManager.MainWindow;
        this.DataContext = _viewModel;
    }
}
```

## Page Implementation Guide

### New Page Checklist
1. Create XAML Page
2. Create ViewModel
3. Register in DI
4. Implement Navigation

### Example Implementation

1. **Create Page**
```xaml
<Page x:Class="MI_GUI_WinUI.Pages.NewFeaturePage">
    <Grid>
        <!-- UI Content -->
    </Grid>
</Page>
```

2. **Create ViewModel**
```csharp
public partial class NewFeatureViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title;
    
    public override async Task InitializeAsync()
    {
        // Initialize state
    }
}
```

3. **Register in DI (App.xaml.cs)**
```csharp
services.AddSingleton<NewFeatureViewModel>();
services.AddTransient<NewFeaturePage>();
```

4. **Implement Navigation**
```csharp
_navigationService.Navigate<NewFeaturePage, NewFeatureViewModel>();
```

## Best Practices

### 1. Window Management
- Always use WindowManager for window operations
- Handle window state persistence
- Implement proper cleanup
- Use weak references for window references in ViewModels

### 2. Navigation
- Use type-safe navigation with ViewModels
- Handle navigation events for cleanup
- Implement proper back navigation
- Consider navigation parameters

### 3. ViewModel Implementation
- Use ObservableObject base class
- Implement INotifyPropertyChanged
- Use [ObservableProperty] attribute for properties
- Implement async initialization
- Handle cleanup properly

### 4. Error Handling
```csharp
try
{
    await _viewModel.InitializeAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error initializing ViewModel");
    _viewModel.ErrorMessage = "Error initializing page.";
}
```

### 5. Resource Management
- Dispose of resources in Cleanup()
- Use weak references for window references
- Clear collections in ViewModels
- Unsubscribe from events

### 6. UI Performance
- Use virtualization for large collections
- Implement async loading patterns
- Cache resources appropriately
- Use proper XAML resource management

### 7. Testing
- Design for testability
- Use dependency injection
- Mock services
- Test ViewModels independently

### 8. Logging
- Use structured logging
- Log meaningful events
- Include context information
- Handle exceptions properly

## Examples

### Complete Page Implementation
```csharp
public sealed partial class FeaturePage : Page
{
    private FeatureViewModel? _viewModel;

    public FeaturePage()
    {
        this.InitializeComponent();
        if (_viewModel == null)
        {
            _viewModel = Ioc.Default.GetRequiredService<FeatureViewModel>();
            SetViewModel(_viewModel);
        }
    }

    private void SetViewModel(FeatureViewModel viewModel)
    {
        _viewModel = viewModel;
        var windowManager = Ioc.Default.GetRequiredService<WindowManager>();
        _viewModel.Window = windowManager.MainWindow;
        this.DataContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await _viewModel?.InitializeAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _viewModel?.Cleanup();
    }
}
```

This architecture document serves as a guide for maintaining consistency and best practices across the application. Follow these patterns when implementing new features or modifying existing ones.