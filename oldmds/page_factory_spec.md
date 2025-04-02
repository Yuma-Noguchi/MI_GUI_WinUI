# Page Factory Specification

## Overview
The PageFactory component will provide a centralized mechanism for creating pages and their associated view models, replacing direct usage of Ioc.Default in the codebase.

## Interface Definition

```csharp
public interface IPageFactory
{
    /// <summary>
    /// Creates a page instance with its dependencies injected
    /// </summary>
    /// <typeparam name="TPage">The type of page to create</typeparam>
    /// <returns>An initialized page instance</returns>
    TPage CreatePage<TPage>() where TPage : Page;

    /// <summary>
    /// Creates a page instance with its associated ViewModel
    /// </summary>
    /// <typeparam name="TPage">The type of page to create</typeparam>
    /// <typeparam name="TViewModel">The type of ViewModel to associate</typeparam>
    /// <returns>An initialized page instance with ViewModel set</returns>
    TPage CreatePage<TPage, TViewModel>() 
        where TPage : Page
        where TViewModel : class;
}
```

## Implementation Details

### PageFactory Class

```csharp
public class PageFactory : IPageFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PageFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TPage CreatePage<TPage>() where TPage : Page
    {
        return ActivatorUtilities.CreateInstance<TPage>(_serviceProvider);
    }

    public TPage CreatePage<TPage, TViewModel>() 
        where TPage : Page
        where TViewModel : class
    {
        var page = CreatePage<TPage>();
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // Set ViewModel using reflection or DataContext
        page.DataContext = viewModel;
        
        return page;
    }
}
```

## Registration

Add to service collection in App.xaml.cs:

```csharp
services.AddSingleton<IPageFactory, PageFactory>();
```

## Usage Examples

### In NavigationService

```csharp
public class NavigationService : INavigationService 
{
    private readonly IPageFactory _pageFactory;

    public NavigationService(IPageFactory pageFactory)
    {
        _pageFactory = pageFactory;
    }

    public bool Navigate<TPage, TViewModel>() 
        where TPage : Page
        where TViewModel : class
    {
        var page = _pageFactory.CreatePage<TPage, TViewModel>();
        return Frame.Navigate(page);
    }
}
```

### In MainWindow

```csharp
public sealed partial class MainWindow : Window
{
    private readonly IPageFactory _pageFactory;

    public MainWindow(IPageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        this.InitializeComponent();
        
        var homePage = _pageFactory.CreatePage<HomePage, HomeViewModel>();
        ContentFrame.Navigate(homePage);
    }
}
```

## Testing Considerations

1. **Mock Implementation**
```csharp
public class MockPageFactory : IPageFactory
{
    public TPage CreatePage<TPage>() where TPage : Page
    {
        // Return mock page for testing
        return Mock.Of<TPage>();
    }

    public TPage CreatePage<TPage, TViewModel>() 
        where TPage : Page
        where TViewModel : class
    {
        var page = CreatePage<TPage>();
        page.DataContext = Mock.Of<TViewModel>();
        return page;
    }
}
```

2. **Test Examples**
```csharp
[TestMethod]
public void NavigationService_Navigate_UsesPageFactory()
{
    // Arrange
    var mockPageFactory = new Mock<IPageFactory>();
    var navigationService = new NavigationService(mockPageFactory.Object);

    // Act
    navigationService.Navigate<HomePage, HomeViewModel>();

    // Assert
    mockPageFactory.Verify(f => 
        f.CreatePage<HomePage, HomeViewModel>(), 
        Times.Once);
}
```

## Migration Steps

1. Create IPageFactory interface and PageFactory implementation
2. Register in DI container
3. Update NavigationService to use IPageFactory
4. Update MainWindow and other direct page creation points
5. Remove Ioc.Default usage from page creation code
6. Add unit tests for PageFactory
7. Add integration tests for navigation flow

## Impact Analysis

### Benefits
- Centralized page creation logic
- Easier testing through abstraction
- Removes static dependencies
- Consistent page initialization

### Risks
- Breaking change for existing page creation
- Need to update all page navigation code
- Potential initialization order issues
- Required testing of all navigation paths

## Future Considerations

1. Support for page parameters
2. Caching frequently used pages
3. Lazy loading of pages and view models
4. Error handling and logging
5. Performance monitoring
6. State management integration

This specification provides the foundation for implementing the PageFactory pattern, which will help eliminate static IoC dependencies and improve testability across the application.