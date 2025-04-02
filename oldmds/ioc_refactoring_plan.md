# Static IoC Container Refactoring Plan

## Current Issues
- Static `Ioc.Default` usage creates tight coupling
- Makes unit testing difficult due to global state
- Violates dependency injection principles
- Harder to track dependencies

## Refactoring Strategy

### Phase 1: Analysis & Preparation
1. **Identify Usage Points**
   - Scan for all `Ioc.Default` usages
   - Document affected components
   - Map dependency chains

2. **Design Pattern Selection**
   - Use constructor injection as primary pattern
   - Consider factory pattern for complex instantiation
   - Plan for scoped dependency management

### Phase 2: Implementation

1. **Page Refactoring**
```csharp
// Before
public sealed partial class FeaturePage : Page
{
    private FeatureViewModel? _viewModel;

    public FeaturePage()
    {
        this.InitializeComponent();
        _viewModel = Ioc.Default.GetRequiredService<FeatureViewModel>();
        SetViewModel(_viewModel);
    }
}

// After
public sealed partial class FeaturePage : Page
{
    private readonly FeatureViewModel _viewModel;

    public FeaturePage(FeatureViewModel viewModel)
    {
        this.InitializeComponent();
        _viewModel = viewModel;
        SetViewModel();
    }
}
```

2. **Service Access Refactoring**
```csharp
// Before
public class SomeService
{
    public void DoSomething()
    {
        var otherService = Ioc.Default.GetRequiredService<IOtherService>();
        otherService.Execute();
    }
}

// After
public class SomeService
{
    private readonly IOtherService _otherService;

    public SomeService(IOtherService otherService)
    {
        _otherService = otherService;
    }

    public void DoSomething()
    {
        _otherService.Execute();
    }
}
```

### Phase 3: Page Factory Implementation

1. Create a PageFactory service:
```csharp
public interface IPageFactory
{
    T CreatePage<T>() where T : Page;
}

public class PageFactory : IPageFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PageFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T CreatePage<T>() where T : Page
    {
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider);
    }
}
```

2. Update NavigationService to use PageFactory:
```csharp
public class NavigationService : INavigationService
{
    private readonly IPageFactory _pageFactory;

    public NavigationService(IPageFactory pageFactory)
    {
        _pageFactory = pageFactory;
    }

    public bool Navigate<TPage>() where TPage : Page
    {
        var page = _pageFactory.CreatePage<TPage>();
        // Navigation logic
    }
}
```

### Phase 4: Testing Infrastructure

1. **Test Setup Example**
```csharp
public class FeatureViewModelTests
{
    private Mock<INavigationService> _navigationService;
    private Mock<IOtherService> _otherService;
    private FeatureViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _navigationService = new Mock<INavigationService>();
        _otherService = new Mock<IOtherService>();
        _viewModel = new FeatureViewModel(_navigationService.Object, _otherService.Object);
    }

    [TestMethod]
    public async Task InitializeAsync_LoadsDataCorrectly()
    {
        // Arrange
        _otherService.Setup(s => s.GetDataAsync())
            .ReturnsAsync(new TestData());

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.IsTrue(_viewModel.IsDataLoaded);
    }
}
```

## Implementation Steps

1. **Initial Setup**
   - Add IPageFactory interface and implementation
   - Register PageFactory in DI container
   - Update NavigationService

2. **Incremental Refactoring**
   - Start with one feature module
   - Update its pages and view models
   - Add unit tests for the refactored components
   - Validate functionality
   - Move to next module

3. **Dependencies Clean-up**
   - Remove static Ioc.Default usages
   - Update service registration
   - Implement proper dependency injection

4. **Testing**
   - Create unit tests for refactored components
   - Verify all dependencies are properly injected
   - Ensure no static dependencies remain

## Success Criteria
- No static Ioc.Default usage in the codebase
- All dependencies properly injected via constructors
- Unit tests can be written without static dependencies
- Components can be tested in isolation

## Risk Mitigation
- Refactor one module at a time
- Add tests before refactoring
- Maintain working application throughout
- Regular integration testing

This plan provides a structured approach to removing static dependencies while maintaining application functionality and improving testability.