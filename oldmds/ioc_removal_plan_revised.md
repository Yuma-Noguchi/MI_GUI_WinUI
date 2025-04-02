# Revised IoC Default Removal Plan

## Current Issues

1. **HomePage.xaml.cs**
```csharp
_navigationService = Ioc.Default.GetService<INavigationService>();
_windowManager = Ioc.Default.GetService<IWindowManager>();
// Remove incorrect ViewModel reference
```

2. **PageHeader.xaml.cs**
```csharp
_navigationService = Ioc.Default.GetRequiredService<INavigationService>();
```

## Keep As-Is

**App.xaml.cs**
```csharp
// Keep this as it's the composition root
Ioc.Default.ConfigureServices(_serviceProvider);
```

Rationale for keeping App.xaml.cs configuration:
- It's the application's composition root
- It's a one-time setup at startup
- Common pattern in MVVM applications
- Provides global service access when needed
- Doesn't violate DI principles as it's configuring, not resolving

## Proposed Changes

### 1. HomePage Refactoring

```csharp
public sealed partial class HomePage : Page
{
    private readonly INavigationService _navigationService;
    private readonly IWindowManager _windowManager;

    public HomePage(
        INavigationService navigationService,
        IWindowManager windowManager)
    {
        this.InitializeComponent();
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }
}
```

### 2. PageHeader Refactoring

```csharp
public sealed partial class PageHeader : UserControl
{
    private readonly INavigationService _navigationService;

    public PageHeader(INavigationService navigationService)
    {
        this.InitializeComponent();
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }
}
```

## Implementation Steps

1. Update HomePage
   - Add constructor parameters for required services
   - Remove ViewModel-related code
   - Update DI registration in App.xaml.cs
   - Update any direct instantiation to use PageFactory

2. Update PageHeader
   - Add constructor parameter for NavigationService
   - Update DI registration
   - Update any direct instantiation points

## Testing Strategy

1. Verify HomePage
   - Test page creation through PageFactory
   - Verify navigation service works
   - Verify window manager functions
   - Test page functionality

2. Verify PageHeader
   - Test control creation
   - Verify navigation service integration
   - Test UI interactions

## Success Criteria

1. Service resolution moved to constructor injection where appropriate
2. No Ioc.Default service resolution in components
3. Application functions correctly
4. Clean startup and navigation

This revised plan focuses on the necessary changes while maintaining appropriate use of the IoC container at the application level.