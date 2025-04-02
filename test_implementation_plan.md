# Test Implementation Plan

## Test Project Setup

Create a new test project:
```
MI_GUI_WinUI.Tests/
├── ServiceTests/
│   ├── PageFactoryTests.cs
│   ├── NavigationServiceTests.cs
│   └── WindowManagerTests.cs
└── TestUtils/
    ├── MockPageFactory.cs
    └── TestBase.cs
```

## Test Categories

### 1. PageFactory Tests
- Test CreatePage<TPage>()
  - Verify page creation
  - Verify dependency injection
  - Test error handling for missing dependencies
- Test CreatePage<TPage, TViewModel>()
  - Verify page and ViewModel creation
  - Verify ViewModel assignment to DataContext
  - Test error cases

### 2. Navigation Service Tests
- Test page navigation with PageFactory
- Test window registration/unregistration
- Test frame management
- Test navigation state management
- Test async navigation operations

### 3. Window Manager Tests
- Test window creation with dependencies
- Test window lifecycle management
- Test state persistence
- Test window activation

## Test Implementation Steps

1. Create Test Project
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0-windows10.0.19041.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.1.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="2.2.8" />
    <PackageReference Include="MSTest.TestFramework" Version="2.2.8" />
    <PackageReference Include="Moq" Version="4.18.1" />
  </ItemGroup>
</Project>
```

2. Implement Mock Classes
```csharp
public class MockPageFactory : IPageFactory
{
    public TPage CreatePage<TPage>() where TPage : Page
    {
        // Return mock page
        return Mock.Of<TPage>();
    }

    public TPage CreatePage<TPage, TViewModel>() 
        where TPage : Page
        where TViewModel : class
    {
        var page = Mock.Of<TPage>();
        var viewModel = Mock.Of<TViewModel>();
        // Set DataContext
        return page;
    }
}
```

3. Sample Test Cases
```csharp
[TestClass]
public class PageFactoryTests
{
    [TestMethod]
    public void CreatePage_WithValidType_ReturnsPage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<HomePage>();
        var serviceProvider = services.BuildServiceProvider();
        var pageFactory = new PageFactory(serviceProvider);

        // Act
        var page = pageFactory.CreatePage<HomePage>();

        // Assert
        Assert.IsNotNull(page);
        Assert.IsInstanceOfType(page, typeof(HomePage));
    }

    [TestMethod]
    public void CreatePage_WithViewModel_SetsDataContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<HomePage>();
        services.AddTransient<HomeViewModel>();
        var serviceProvider = services.BuildServiceProvider();
        var pageFactory = new PageFactory(serviceProvider);

        // Act
        var page = pageFactory.CreatePage<HomePage, HomeViewModel>();

        // Assert
        Assert.IsNotNull(page);
        Assert.IsNotNull(page.DataContext);
        Assert.IsInstanceOfType(page.DataContext, typeof(HomeViewModel));
    }
}
```

## Code Coverage Goals

- PageFactory: 100% coverage
- NavigationService: 90% coverage
- WindowManager: 85% coverage

## Testing Timeline

1. Day 1: Setup test project and implement base test utilities
2. Day 2: Implement PageFactory tests
3. Day 3: Implement NavigationService tests
4. Day 4: Implement WindowManager tests
5. Day 5: Integration tests and coverage analysis

## Success Criteria

1. All unit tests pass
2. Code coverage meets or exceeds goals
3. All edge cases and error conditions are tested
4. CI pipeline runs tests successfully
5. No regression in existing functionality

This plan provides a structured approach to testing our refactored components while ensuring proper coverage and quality.