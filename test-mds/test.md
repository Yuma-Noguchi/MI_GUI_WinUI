# Testing Strategy for MI_GUI_WinUI

## Testing Architecture

Here's a comprehensive testing plan focusing on unit and integration tests:

### 1. Project Structure

```
MI_GUI_WinUI.Tests/
├── Services/
│   ├── ActionServiceTests.cs
│   ├── NavigationServiceTests.cs
│   ├── WindowManagerTests.cs
│   ├── StableDiffusionServiceTests.cs
│   └── LoggingServiceTests.cs
├── ViewModels/
│   ├── IconStudioViewModelTests.cs
│   ├── ProfileEditorViewModelTests.cs
│   └── NavigationViewModelTests.cs
├── Models/
│   ├── ProfileModelTests.cs
│   └── ActionModelTests.cs
├── Helpers/
│   └── TestHelpers.cs
└── Mocks/
    └── MockServices.cs
```

## Implementation Guide

### Step 1: Create Test Project

```bash
dotnet new mstest -n MI_GUI_WinUI.Tests
```

### Step 2: Add NuGet Packages

```bash
cd MI_GUI_WinUI.Tests
dotnet add package Moq
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package MSTest.TestAdapter
dotnet add package MSTest.TestFramework
dotnet add package Microsoft.Extensions.DependencyInjection
```

### Step 3: Add Project Reference

```bash
dotnet add reference ../MI_GUI_WinUI/MI_GUI_WinUI.csproj
```

### Step 4: Implement Mock Services

````csharp
using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Services;
using Moq;
using System;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Mocks
{
    public static class MockServices
    {
        public static Mock<INavigationService> CreateMockNavigationService()
        {
            var mock = new Mock<INavigationService>();
            mock.Setup(m => m.NavigateTo(It.IsAny<Type>(), It.IsAny<object>()))
                .Returns(true);
            return mock;
        }

        public static Mock<IWindowManager> CreateMockWindowManager()
        {
            var mock = new Mock<IWindowManager>();
            return mock;
        }

        public static Mock<ILoggingService> CreateMockLoggingService()
        {
            var mock = new Mock<ILoggingService>();
            mock.Setup(m => m.LogInformation(It.IsAny<string>()));
            mock.Setup(m => m.LogError(It.IsAny<string>(), It.IsAny<Exception>()));
            return mock;
        }
        
        public static Mock<IActionService> CreateMockActionService()
        {
            var mock = new Mock<IActionService>();
            return mock;
        }
        
        public static Mock<IStableDiffusionService> CreateMockStableDiffusionService()
        {
            var mock = new Mock<IStableDiffusionService>();
            mock.Setup(m => m.GenerateImageAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new byte[0]);
            return mock;
        }
    }
}
````

### Step 5: Core Service Tests

#### NavigationService Tests

````csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Tests.Mocks;
using MI_GUI_WinUI.Views;
using Moq;
using System;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class NavigationServiceTests
    {
        private Mock<ILoggingService> _mockLoggingService;
        private NavigationService _navigationService;

        [TestInitialize]
        public void Initialize()
        {
            _mockLoggingService = MockServices.CreateMockLoggingService();
            
            // You'll need to create a NavigationService properly with Frame
            // This is a simplified version - actual implementation will depend on your constructor
            _navigationService = new NavigationService(_mockLoggingService.Object);
        }

        [TestMethod]
        public void RegisterPage_RegistersPageType()
        {
            // Arrange
            string key = "TestPage";
            Type pageType = typeof(IconStudioPage);

            // Act
            _navigationService.Register(key, pageType);
            bool result = _navigationService.NavigateTo(key);

            // Assert
            Assert.IsTrue(result);
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void NavigateTo_UnregisteredPage_ReturnsFalse()
        {
            // Act
            bool result = _navigationService.NavigateTo("UnregisteredPage");

            // Assert
            Assert.IsFalse(result);
            _mockLoggingService.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }
    }
}
````

#### WindowManager Tests

````csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Tests.Mocks;
using Moq;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class WindowManagerTests
    {
        private Mock<ILoggingService> _mockLoggingService;
        private WindowManager _windowManager;

        [TestInitialize]
        public void Initialize()
        {
            _mockLoggingService = MockServices.CreateMockLoggingService();
            _windowManager = new WindowManager(_mockLoggingService.Object);
        }

        [TestMethod]
        public void SaveWindowState_ShouldLogInformation()
        {
            // Arrange
            var windowId = "MainWindow";
            var width = 800;
            var height = 600;
            var left = 100;
            var top = 100;
            var windowState = WindowState.Normal;
            
            // Act
            _windowManager.SaveWindowState(windowId, width, height, left, top, windowState);
            
            // Assert
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.Once);
        }
        
        [TestMethod]
        public void GetWindowState_NonExistentWindow_ReturnsDefaultValues()
        {
            // Act
            var state = _windowManager.GetWindowState("NonExistentWindow");
            
            // Assert
            Assert.IsNotNull(state);
            Assert.AreEqual(800, state.Width); // Assuming default width is 800
            Assert.AreEqual(600, state.Height); // Assuming default height is 600
        }
    }
}
````

### Step 6: ViewModel Tests

````csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Tests.Mocks;
using MI_GUI_WinUI.ViewModels;
using Moq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.ViewModels
{
    [TestClass]
    public class IconStudioViewModelTests
    {
        private IconStudioViewModel _viewModel;
        private Mock<INavigationService> _mockNavigationService;
        private Mock<IStableDiffusionService> _mockStableDiffusionService;
        private Mock<ILoggingService> _mockLoggingService;

        [TestInitialize]
        public void Initialize()
        {
            _mockNavigationService = MockServices.CreateMockNavigationService();
            _mockStableDiffusionService = MockServices.CreateMockStableDiffusionService();
            _mockLoggingService = MockServices.CreateMockLoggingService();
            
            _viewModel = new IconStudioViewModel(
                _mockNavigationService.Object,
                _mockStableDiffusionService.Object,
                _mockLoggingService.Object
            );
        }

        [TestMethod]
        public async Task GenerateImage_ShouldCallStableDiffusionService()
        {
            // Arrange
            string prompt = "Test prompt";
            
            // Act
            await _viewModel.GenerateImageCommand.ExecuteAsync(prompt);
            
            // Assert
            _mockStableDiffusionService.Verify(s => 
                s.GenerateImageAsync(It.Is<string>(p => p.Contains(prompt)), It.IsAny<int>()),
                Times.Once);
        }

        [TestMethod]
        public void NavigateBack_ShouldCallNavigationService()
        {
            // Act
            _viewModel.NavigateBackCommand.Execute(null);
            
            // Assert
            _mockNavigationService.Verify(n => n.GoBack(), Times.Once);
        }
    }
}
````

### Step 7: Model Tests

````csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using System.Collections.Generic;
using System.Linq;

namespace MI_GUI_WinUI.Tests.Models
{
    [TestClass]
    public class ProfileModelTests
    {
        [TestMethod]
        public void ProfileModel_PropertiesSetCorrectly()
        {
            // Arrange
            string name = "Test Profile";
            string description = "Test Description";
            var actions = new List<ActionModel> { new ActionModel { Name = "Test Action" } };
            
            // Act
            var profile = new ProfileModel
            {
                Name = name,
                Description = description,
                Actions = actions
            };
            
            // Assert
            Assert.AreEqual(name, profile.Name);
            Assert.AreEqual(description, profile.Description);
            Assert.AreEqual(1, profile.Actions.Count);
            Assert.AreEqual("Test Action", profile.Actions.First().Name);
        }
        
        [TestMethod]
        public void ProfileModel_Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new ProfileModel
            {
                Name = "Original Profile",
                Description = "Original Description",
                Actions = new List<ActionModel> { new ActionModel { Name = "Original Action" } }
            };
            
            // Act
            var clone = original.Clone();
            
            // Assert
            Assert.AreEqual(original.Name, clone.Name);
            Assert.AreEqual(original.Description, clone.Description);
            Assert.AreEqual(original.Actions.Count, clone.Actions.Count);
            Assert.AreEqual(original.Actions[0].Name, clone.Actions[0].Name);
            
            // Verify it's a deep copy
            clone.Name = "Modified";
            clone.Actions[0].Name = "Modified Action";
            
            Assert.AreEqual("Original Profile", original.Name);
            Assert.AreEqual("Original Action", original.Actions[0].Name);
        }
    }
}
````

### Step 8: Integration Tests for Services

````csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Tests.Mocks;
using MI_GUI_WinUI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class ActionServiceTests
    {
        private Mock<ILoggingService> _mockLoggingService;
        private ActionService _actionService;

        [TestInitialize]
        public void Initialize()
        {
            _mockLoggingService = MockServices.CreateMockLoggingService();
            _actionService = new ActionService(_mockLoggingService.Object);
        }

        [TestMethod]
        public async Task LoadProfiles_LoadsProfilesCorrectly()
        {
            // Act
            var profiles = await _actionService.LoadProfilesAsync();
            
            // Assert
            Assert.IsNotNull(profiles);
            Assert.IsTrue(profiles.Count > 0);
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.AtLeastOnce);
        }
        
        [TestMethod]
        public async Task SaveProfile_SavesProfileCorrectly()
        {
            // Arrange
            var profile = new ProfileModel
            {
                Name = "Test Profile",
                Description = "Test Description",
                Actions = new List<ActionModel>
                {
                    new ActionModel { Name = "Test Action" }
                }
            };
            
            // Act
            await _actionService.SaveProfileAsync(profile);
            var loadedProfiles = await _actionService.LoadProfilesAsync();
            var savedProfile = loadedProfiles.FirstOrDefault(p => p.Name == "Test Profile");
            
            // Assert
            Assert.IsNotNull(savedProfile);
            Assert.AreEqual(profile.Description, savedProfile.Description);
            Assert.AreEqual(1, savedProfile.Actions.Count);
            Assert.AreEqual("Test Action", savedProfile.Actions.First().Name);
        }
    }
}
````

## Advanced Tests

### UI and Integration Testing

For UI testing in WinUI 3, consider adding:

1. **UI Automation Tests** using Microsoft UI Automation framework
2. **Snapshot/Visual Regression Tests** for UI components
3. **End-to-end workflow tests** covering complete user scenarios

### Testing Tools Worth Adding

1. **FluentAssertions** for more readable assertions:
   ```bash
   dotnet add package FluentAssertions
   ```

2. **Verify** for snapshot testing:
   ```bash
   dotnet add package Verify.MSTest
   ```

## Running Tests

Run tests with:
```bash
dotnet test MI_GUI_WinUI.Tests
```

Or through Visual Studio's Test Explorer.

## Testing Best Practices

1. **Arrange-Act-Assert** pattern for test structure
2. **Test isolation** - each test should run independently
3. **Mock external dependencies** rather than testing real services
4. **Test both positive and negative scenarios**
5. **Parameterized tests** for testing multiple input combinations

Would you like me to elaborate on any particular test implementation or create additional test examples for specific components?