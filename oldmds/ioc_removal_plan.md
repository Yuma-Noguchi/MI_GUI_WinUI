# IoC Default Removal Plan

## Current Issues

1. **HomePage.xaml.cs**
```csharp
_navigationService = Ioc.Default.GetService<INavigationService>();
_windowManager = Ioc.Default.GetService<IWindowManager>();
_viewModel = Ioc.Default.GetService<MainWindowViewModel>();
```

2. **PageHeader.xaml.cs**
```csharp
_navigationService = Ioc.Default.GetRequiredService<INavigationService>();
```

3. **App.xaml.cs**
```csharp
Ioc.Default.ConfigureServices(_serviceProvider);
```

## Proposed Changes

### 1. HomePage Refactoring

Convert HomePage to use constructor injection:

```csharp
public sealed partial class HomePage : Page
{
    private readonly INavigationService _navigationService;
    private readonly IWindowManager _windowManager;
    private readonly MainWindowViewModel _viewModel;

    public HomePage(
        INavigationService navigationService,
        IWindowManager windowManager,
        MainWindowViewModel viewModel)
    {
        this.InitializeComponent();
        _navigationService = navigationService;
        _windowManager = windowManager;
        _viewModel = viewModel;
        
        DataContext = _viewModel;
    }
}
```

### 2. PageHeader Refactoring

Convert PageHeader to use constructor injection:

```csharp
public sealed partial class PageHeader : UserControl
{
    private readonly INavigationService _navigationService;

    public PageHeader(INavigationService navigationService)
    {
        this.InitializeComponent();
        _navigationService = navigationService;
    }
}
```

### 3. Application Service Management

Replace global IoC container with direct ServiceProvider usage:

1. Add public Services property to App class (already exists)
2. Use Services property directly when needed instead of Ioc.Default
3. Remove Ioc.Default.ConfigureServices call

## Implementation Steps

1. Update HomePage
   - Add constructor parameters
   - Update DI registration in App.xaml.cs
   - Update any direct instantiation to use PageFactory

2. Update PageHeader
   - Add constructor parameters
   - Update DI registration in App.xaml.cs
   - Update any direct instantiation to use constructor injection

3. Remove Global IoC Usage
   - Remove Ioc.Default.ConfigureServices line
   - Update any remaining service resolutions to use constructor injection
   - Add XML comments explaining service access patterns

## Testing Strategy

1. Verify HomePage Navigation
   - Test page creation through PageFactory
   - Verify all services are properly injected
   - Test page functionality

2. Verify PageHeader
   - Test control creation
   - Verify navigation service integration
   - Test UI interactions

3. General Testing
   - Verify application startup
   - Test navigation flows
   - Verify window management
   - Test service resolution

## Success Criteria

1. All Ioc.Default usages removed
2. All components use constructor injection
3. Application functions correctly
4. No runtime service resolution errors
5. Clean startup and navigation

## Migration Notes

- Maintain backward compatibility during transition
- Update documentation to reflect new patterns
- Consider adding service locator anti-pattern warnings in code comments
- Add architectural decision record (ADR) explaining the change

This plan provides a structured approach to removing the remaining Ioc.Default usages while maintaining application functionality and improving testability.