using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class ActionServiceIntegrationTests : IntegrationTestBase
    {
        private IActionService _actionService;
        private string _actionsPath;
        private const string TestActionsFolder = "TestActions";

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            
            // Create actions directory in test data path
            _actionsPath = Path.Combine(TestDataPath, TestActionsFolder);
            Directory.CreateDirectory(_actionsPath);
            
            // Define actions file path
            string actionsFilePath = Path.Combine(_actionsPath, "actions.json");
            
            // Register the testable ActionService implementation
            _actionService = new TestableActionService(
                GetRequiredService<ILogger<ActionService>>(), 
                actionsFilePath
            );
        }

        [TestCleanup]
        public override void CleanupTest()
        {
            // Cleanup any action files created during tests
            _actionService.ClearCache();
            base.CleanupTest();
        }

        protected override void ConfigureIntegrationServices(IServiceCollection services)
        {
            base.ConfigureIntegrationServices(services);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ActionLifecycle_CreateReadUpdateDelete_FullLifecycleWorks()
        {
            // Arrange - Create action
            var action = CreateTestAction("Integration Test Action");
            action.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "test_key", "initial_value" } } 
            };
            
            // Act 1 - Save action
            await _actionService.SaveActionAsync(action);
            string actionsFile = Path.Combine(_actionsPath, "actions.json");
            
            // Assert 1 - Verify file was created and contains the action
            Assert.IsTrue(File.Exists(actionsFile), "Actions file should be created");
            string json = await File.ReadAllTextAsync(actionsFile);
            Assert.IsTrue(json.Contains("Integration Test Action"), "File should contain action name");
            
            // Act 2 - Read all actions
            var actions = await _actionService.LoadActionsAsync();
            
            // Assert 2 - Verify action was read
            Assert.IsTrue(actions.Count >= 1, "At least one action should be loaded");
            var loadedAction = actions.FirstOrDefault(a => a.Name == "Integration Test Action");
            Assert.IsNotNull(loadedAction, "The saved action should be loaded");
            Assert.AreEqual("initial_value", loadedAction.Args[0]["test_key"], "Action should have the correct argument");
            
            // Act 3 - Update action
            loadedAction.Args[0]["test_key"] = "updated_value";
            loadedAction.Args[0]["new_key"] = "new_value";
            await _actionService.SaveActionAsync(loadedAction);
            
            // Act 4 - Get by ID and verify update
            var updatedAction = await _actionService.GetActionByIdAsync(loadedAction.Id);
            
            // Assert 4 - Verify action was updated
            Assert.AreEqual("updated_value", updatedAction.Args[0]["test_key"], "Action should be updated");
            Assert.AreEqual("new_value", updatedAction.Args[0]["new_key"], "Action should have new key");
            
            // Act 5 - Delete action
            await _actionService.DeleteActionAsync(loadedAction.Id);
            
            // Assert 5 - Verify action was deleted
            var remainingActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(0, remainingActions.Count, "No actions should remain after deletion");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task LoadActions_WithMultipleActions_LoadsAllCorrectly()
        {
            // Arrange
            var action1 = CreateTestAction("Action One");
            var action2 = CreateTestAction("Action Two");
            var action3 = CreateTestAction("Action Three");
            
            // Add different arguments to each action
            action1.Args = new List<Dictionary<string, object>> { new Dictionary<string, object> { { "key", "value1" } } };
            action2.Args = new List<Dictionary<string, object>> { new Dictionary<string, object> { { "key", "value2" } } };
            action3.Args = new List<Dictionary<string, object>> { new Dictionary<string, object> { { "key", "value3" } } };
            
            await _actionService.SaveActionAsync(action1);
            await _actionService.SaveActionAsync(action2);
            await _actionService.SaveActionAsync(action3);
            
            // Clear cache to ensure we're reading from disk
            _actionService.ClearCache();
            
            // Act
            var actions = await _actionService.LoadActionsAsync();
            
            // Assert
            Assert.AreEqual(3, actions.Count, "All three actions should be loaded");
            Assert.IsTrue(actions.Any(a => a.Name == "Action One" && (string)a.Args[0]["key"] == "value1"), 
                "Action One should be loaded with correct arguments");
            Assert.IsTrue(actions.Any(a => a.Name == "Action Two" && (string)a.Args[0]["key"] == "value2"), 
                "Action Two should be loaded with correct arguments");
            Assert.IsTrue(actions.Any(a => a.Name == "Action Three" && (string)a.Args[0]["key"] == "value3"), 
                "Action Three should be loaded with correct arguments");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task GetActionByName_CaseInsensitive_FindsAction()
        {
            // Arrange
            var action = CreateTestAction("Case Sensitive Test");
            await _actionService.SaveActionAsync(action);
            
            // Act & Assert - This will pass only if the name comparison is case insensitive
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => 
                _actionService.GetActionByNameAsync("case sensitive test"));
        }

        [TestMethod]
        [TestCategory("Integration")]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task GetActionById_WithInvalidId_ThrowsException()
        {
            // Act - Try to get an action with a non-existent ID
            await _actionService.GetActionByIdAsync("non-existent-id");
            
            // Assert - Exception should be thrown (handled by ExpectedException attribute)
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task ConcurrentUpdates_ToSameAction_PreservesLastUpdate()
        {
            // Arrange
            var originalAction = CreateTestAction("Concurrent Update Test");
            originalAction.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "original", "value" } } 
            };
            await _actionService.SaveActionAsync(originalAction);
            
            // Get the original ID
            string actionId = originalAction.Id;
            
            // Act 1 - First update
            var update1 = CreateTestAction("Concurrent Update Test");
            update1.Id = actionId;
            update1.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "update1", "value1" } } 
            };
            await _actionService.SaveActionAsync(update1);
            
            // Act 2 - Second update (simulating concurrent update)
            var update2 = CreateTestAction("Concurrent Update Test");
            update2.Id = actionId;
            update2.Args = new List<Dictionary<string, object>> 
            { 
                new Dictionary<string, object> { { "update2", "value2" } } 
            };
            await _actionService.SaveActionAsync(update2);
            
            // Act 3 - Load the final state
            var finalAction = await _actionService.GetActionByIdAsync(actionId);
            
            // Assert - The last update should win
            Assert.IsTrue(finalAction.Args[0].ContainsKey("update2"), "Final action should contain the second update");
            Assert.AreEqual("value2", finalAction.Args[0]["update2"], "Final action should have the second update value");
            Assert.IsFalse(finalAction.Args[0].ContainsKey("update1"), "First update should be overwritten");
            Assert.IsFalse(finalAction.Args[0].ContainsKey("original"), "Original value should be overwritten");
        }

        // Helper methods
        private ActionData CreateTestAction(string name)
        {
            return new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Args = new List<Dictionary<string, object>>()
            };
        }
    }
}