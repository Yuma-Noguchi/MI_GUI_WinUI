using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.ViewModels.Base;
using Moq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Provides utility methods and mock objects for testing
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a mock logger for the specified type
        /// </summary>
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a mock navigation service
        /// </summary>
        public static Mock<INavigationService> CreateMockNavigationService()
        {
            var mock = new Mock<INavigationService>();
            mock.Setup(x => x.Navigate<Page>(It.IsAny<object>())).Returns(true);
            mock.Setup(x => x.GoBack()).Returns(true);
            return mock;
        }

        /// <summary>
        /// Creates a mock action service
        /// </summary>
        public static Mock<IActionService> CreateMockActionService()
        {
            var mock = new Mock<IActionService>();
            mock.Setup(x => x.LoadActionsAsync())
                .ReturnsAsync(new List<ActionData>());
            return mock;
        }

        /// <summary>
        /// Creates a mock stable diffusion service
        /// </summary>
        public static Mock<IStableDiffusionService> CreateMockStableDiffusionService()
        {
            var mock = new Mock<IStableDiffusionService>();
            mock.Setup(x => x.IsInitialized).Returns(true);
            mock.Setup(x => x.Initialize(false))
                .Returns(Task.CompletedTask);
            return mock;
        }

        /// <summary>
        /// Creates a temporary directory for testing file operations
        /// </summary>
        public static string CreateTestDirectory()
        {
            string tempPath = Path.Combine(
                Path.GetTempPath(),
                $"MI_GUI_WinUI_Tests_{Guid.NewGuid()}");
            
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Cleans up a test directory
        /// </summary>
        public static void CleanupTestDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception)
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        

        /// <summary>
        /// Creates a test action with the specified name
        /// </summary>
        public static ActionData CreateTestAction(string name = "Test Action")
        {
            return new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Args = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "TestParam", "TestValue" }
                    }
                }
            };
        }
    }
}