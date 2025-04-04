using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class ActionServiceTests : UnitTestBase
    {
        private IActionService _actionService;
        private string _testActionsPath;
        private Mock<ILogger<ActionService>> _mockLogger;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            _testActionsPath = Path.Combine(TestDirectory, "Actions", "actions.json");
            
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_testActionsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            else
            {
                // Clean up any existing files to start fresh
                if (File.Exists(_testActionsPath))
                {
                    File.Delete(_testActionsPath);
                }
            }
            
            _mockLogger = new Mock<ILogger<ActionService>>();
            
            // Use the TestableActionService instead of the real ActionService
            var testActionService = new TestableActionService(_mockLogger.Object);
            testActionService.SetActionsFilePath(_testActionsPath);
            _actionService = testActionService;
        }
        
        [TestMethod]
        public async Task LoadActionsAsync_WhenNoActionsExist_ReturnsEmptyList()
        {
            // Act
            var result = await _actionService.LoadActionsAsync();
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(0, result.Count, "Result should be an empty list");
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Actions file not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task LoadActionsAsync_WithExistingActions_LoadsSuccessfully()
        {
            // Arrange
            var action1 = CreateTestAction("Test Action 1");
            var action2 = CreateTestAction("Test Action 2");
            await _actionService.SaveActionAsync(action1);
            await _actionService.SaveActionAsync(action2);
            
            // Clear the cache
            _actionService.ClearCache();
            
            // Act
            var result = await _actionService.LoadActionsAsync();
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(2, result.Count, "Result should contain 2 actions");
            Assert.IsTrue(result.Any(a => a.Name == "Test Action 1"), "Result should contain action 1");
            Assert.IsTrue(result.Any(a => a.Name == "Test Action 2"), "Result should contain action 2");
        }
        
        [TestMethod]
        public async Task SaveActionAsync_WithValidAction_Succeeds()
        {
            // Arrange
            var action = CreateTestAction("Save Test Action");
            var args = new Dictionary<string, object> { { "press", "back" }, { "sleep", 1.0 } };
            action.Args = new List<Dictionary<string, object>> { args };
            
            // Act
            await _actionService.SaveActionAsync(action);
            
            // Assert
            Assert.IsNotNull(action.Id, "Action should have an ID after saving");
            
            // Verify it can be retrieved
            var loadedActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(1, loadedActions.Count, "Should have one saved action");
            
            var loadedAction = loadedActions.First();
            Assert.AreEqual("Save Test Action", loadedAction.Name, "Action name should match");
            Assert.AreEqual(action.Id, loadedAction.Id, "Action ID should match");
            Assert.AreEqual(1, loadedAction.Args.Count, "Action should have one argument set");
            
            var loadedArgs = loadedAction.Args[0];
            Assert.AreEqual("back", loadedArgs["press"], "Action argument 'back' should match");
            Assert.AreEqual(1.0, loadedArgs["sleep"], "Action argument '1.0' should match");
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Action saved")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task SaveActionAsync_WithNullAction_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                _actionService.SaveActionAsync(null));
        }
        
        [TestMethod]
        public async Task SaveActionAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var action = CreateTestAction("");
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                _actionService.SaveActionAsync(action));
        }
        
        [TestMethod]
        public async Task SaveActionAsync_WithNullArgs_ThrowsException()
        {
            // Arrange
            var action = CreateTestAction("Test Action");
            action.Args = null;
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                _actionService.SaveActionAsync(action));
        }
        
        [TestMethod]
        public async Task SaveActionAsync_UpdatesExistingAction_WhenNameMatches()
        {
            // Arrange
            var originalAction = CreateTestAction("Update Test Action");
            originalAction.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "original", "value" } } 
            };
            await _actionService.SaveActionAsync(originalAction);
            
            // Get the assigned ID
            var originalId = originalAction.Id;
            
            // Create a new action with the same name but different args
            var updatedAction = CreateTestAction("Update Test Action");
            updatedAction.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "updated", "newvalue" } } 
            };
            
            // Act
            await _actionService.SaveActionAsync(updatedAction);
            
            // Assert
            var loadedAction = await _actionService.GetActionByNameAsync("Update Test Action");
            Assert.AreEqual(originalId, loadedAction.Id, "Action ID should not change on update");
            Assert.AreEqual(1, loadedAction.Args.Count, "Action should have one argument set");
            Assert.IsTrue(loadedAction.Args[0].ContainsKey("updated"), "Action should have updated argument key");
            Assert.AreEqual("newvalue", loadedAction.Args[0]["updated"], "Action should have updated argument value");
        }
        
        [TestMethod]
        public async Task DeleteActionAsync_ExistingAction_DeletesSuccessfully()
        {
            // Arrange
            var action = CreateTestAction("Delete Test Action");
            await _actionService.SaveActionAsync(action);
            
            var loadedActionsBefore = await _actionService.LoadActionsAsync();
            Assert.AreEqual(1, loadedActionsBefore.Count, "Setup verification: Should have one action");
            
            // Act
            await _actionService.DeleteActionAsync(action.Id);
            
            // Assert
            var loadedActionsAfter = await _actionService.LoadActionsAsync();
            Assert.AreEqual(0, loadedActionsAfter.Count, "Should have no actions after deletion");
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Action deleted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task DeleteActionAsync_NonexistentAction_ThrowsKeyNotFoundException()
        {
            // Arrange
            string nonExistentId = "non-existent-id";
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() =>
                _actionService.DeleteActionAsync(nonExistentId));
        }
        
        [TestMethod]
        public async Task DeleteActionAsync_EmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                _actionService.DeleteActionAsync(string.Empty));
        }
        
        [TestMethod]
        public async Task GetActionByIdAsync_ExistingId_ReturnsAction()
        {
            // Arrange
            var action = CreateTestAction("Get By Id Test Action");
            await _actionService.SaveActionAsync(action);
            
            // Act
            var result = await _actionService.GetActionByIdAsync(action.Id);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("Get By Id Test Action", result.Name, "Action name should match");
            Assert.AreEqual(action.Id, result.Id, "Action ID should match");
        }
        
        [TestMethod]
        public async Task GetActionByIdAsync_NonexistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            string nonExistentId = "non-existent-id";
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() =>
                _actionService.GetActionByIdAsync(nonExistentId));
        }
        
        [TestMethod]
        public async Task GetActionByIdAsync_EmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                _actionService.GetActionByIdAsync(string.Empty));
        }
        
        [TestMethod]
        public async Task GetActionByNameAsync_ExistingName_ReturnsAction()
        {
            // Arrange
            var action = CreateTestAction("Get By Name Test Action");
            await _actionService.SaveActionAsync(action);
            
            // Act
            var result = await _actionService.GetActionByNameAsync("Get By Name Test Action");
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("Get By Name Test Action", result.Name, "Action name should match");
            Assert.AreEqual(action.Id, result.Id, "Action ID should match");
        }
        
        [TestMethod]
        public async Task GetActionByNameAsync_NonexistentName_ThrowsKeyNotFoundException()
        {
            // Arrange
            string nonExistentName = "non-existent-name";
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() =>
                _actionService.GetActionByNameAsync(nonExistentName));
        }
        
        [TestMethod]
        public async Task GetActionByNameAsync_EmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                _actionService.GetActionByNameAsync(string.Empty));
        }
        
        [TestMethod]
        public async Task ClearCache_RemovesActionsFromMemoryOnly()
        {
            // Arrange
            var action = CreateTestAction("Cache Test Action");
            await _actionService.SaveActionAsync(action);
            
            // Act 1 - Clear the cache
            _actionService.ClearCache();
            
            // Act 2 - Try to get the action (should throw as it's not in memory)
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() =>
                _actionService.GetActionByNameAsync("Cache Test Action"));
            
            // Act 3 - Load actions from disk
            var loadedActions = await _actionService.LoadActionsAsync();
            
            // Assert - The action should still be on disk
            Assert.AreEqual(1, loadedActions.Count, "Should load one action from disk");
            Assert.AreEqual("Cache Test Action", loadedActions[0].Name, "Action name should match");
        }
        
        [TestMethod]
        public async Task ActionLifecycle_CreateReadUpdateDelete_FullLifecycleWorks()
        {
            // Arrange - Create action
            var action = CreateTestAction("Lifecycle Test Action");
            action.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "initial", "value" } } 
            };
            
            // Act 1 - Save action
            await _actionService.SaveActionAsync(action);
            
            // Assert 1 - Verify file was created and has content
            Assert.IsTrue(File.Exists(_testActionsPath), "Action file should be created");
            string fileContent = await File.ReadAllTextAsync(_testActionsPath);
            Assert.IsTrue(fileContent.Contains("Lifecycle Test Action"), "File should contain action name");
            
            // Act 2 - Load actions
            var loadedActions = await _actionService.LoadActionsAsync();
            
            // Assert 2 - Verify action was loaded
            Assert.AreEqual(1, loadedActions.Count, "Should load one action");
            var loadedAction = loadedActions[0];
            Assert.AreEqual("Lifecycle Test Action", loadedAction.Name, "Action name should match");
            Assert.AreEqual("value", loadedAction.Args[0]["initial"], "Action initial value should match");
            
            // Act 3 - Update action
            loadedAction.Args[0]["initial"] = "updated";
            loadedAction.Args[0]["new"] = "added";
            await _actionService.SaveActionAsync(loadedAction);
            
            // Act 4 - Get by ID to verify update
            var updatedAction = await _actionService.GetActionByIdAsync(loadedAction.Id);
            
            // Assert 4 - Verify action was updated
            Assert.AreEqual("updated", updatedAction.Args[0]["initial"], "Action value should be updated");
            Assert.AreEqual("added", updatedAction.Args[0]["new"], "New action value should be added");
            
            // Act 5 - Delete action
            await _actionService.DeleteActionAsync(loadedAction.Id);
            
            // Assert 5 - Verify empty actions list
            var finalActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(0, finalActions.Count, "Should have no actions after deletion");
        }
        
        // Helper methods for test data generation
        private ActionData CreateTestAction(string name)
        {
            return new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Args = new List<Dictionary<string, object>>()
            };
        }
        
        private async Task<ActionData> LoadActionFromFile(string actionId)
        {
            if (!File.Exists(_testActionsPath))
            {
                return null;
            }
            
            string json = await File.ReadAllTextAsync(_testActionsPath);
            var actions = JsonConvert.DeserializeObject<Dictionary<string, ActionData>>(json);
            
            if (actions != null && actions.TryGetValue(actionId, out var action))
            {
                return action;
            }
            
            return null;
        }
    }
}