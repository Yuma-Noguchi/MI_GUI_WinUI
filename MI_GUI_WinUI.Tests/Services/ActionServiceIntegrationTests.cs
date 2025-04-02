using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using System.Text.Json;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class ActionServiceIntegrationTests : IntegrationTestBase
    {
        private IActionService _actionService;
        private string _testActionsPath;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();

            // Set up test actions directory
            _testActionsPath = Path.Combine(TestDataPath, "actions");
            Directory.CreateDirectory(_testActionsPath);

            // Get the real action service from DI
            _actionService = GetRequiredService<IActionService>();
        }

        [TestMethod]
        public async Task FullActionLifecycle_Integration()
        {
            // 1. Create and save a new action
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Integration Test Action",
                Args = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "param1", "value1" },
                        { "param2", 42 }
                    }
                }
            };

            await _actionService.SaveActionAsync(action);

            // 2. Verify the action file was created
            var actionFilePath = Path.Combine(_testActionsPath, "actions.json");
            Assert.IsTrue(File.Exists(actionFilePath), "Action file should exist");

            // 3. Read and verify file content
            var fileContent = await File.ReadAllTextAsync(actionFilePath);
            Assert.IsFalse(string.IsNullOrEmpty(fileContent), "File content should not be empty");

            // 4. Load actions and verify content
            var loadedActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(1, loadedActions.Count, "Should have one action");
            
            var loadedAction = loadedActions[0];
            Assert.AreEqual(action.Id, loadedAction.Id);
            Assert.AreEqual(action.Name, loadedAction.Name);
            Assert.AreEqual(2, loadedAction.Args.Count);
            Assert.AreEqual("value1", loadedAction.Args[0]["param1"]);
            Assert.AreEqual(42, loadedAction.Args[0]["param2"]);

            // 5. Update the action
            action.Args[0]["param1"] = "updated value";
            await _actionService.SaveActionAsync(action);

            // 6. Verify update
            var updatedAction = await _actionService.GetActionByIdAsync(action.Id);
            Assert.AreEqual("updated value", updatedAction.Args[0]["param1"]);

            // 7. Delete the action
            await _actionService.DeleteActionAsync(action.Id);

            // 8. Verify deletion
            var finalActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(0, finalActions.Count, "Should have no actions after deletion");
        }

        [TestMethod]
        public async Task ConcurrentAccess_Integration()
        {
            // Create multiple actions concurrently
            var tasks = new List<Task>();
            var actions = new List<ActionData>();

            for (int i = 0; i < 10; i++)
            {
                var action = new ActionData
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Concurrent Action {i}",
                    Args = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "index", i }
                        }
                    }
                };
                actions.Add(action);
                tasks.Add(_actionService.SaveActionAsync(action));
            }

            // Wait for all saves to complete
            await Task.WhenAll(tasks);

            // Verify all actions were saved
            var loadedActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(10, loadedActions.Count, "All actions should be saved");

            // Verify each action was saved correctly
            foreach (var action in actions)
            {
                var loadedAction = await _actionService.GetActionByNameAsync(action.Name);
                Assert.IsNotNull(loadedAction, $"Action {action.Name} should exist");
                Assert.AreEqual(action.Id, loadedAction.Id);
            }
        }

        [TestMethod]
        public async Task FileSystem_Integration()
        {
            // 1. Save an action
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "File System Test Action",
                Args = new List<Dictionary<string, object>>()
            };

            await _actionService.SaveActionAsync(action);

            // 2. Verify file exists
            var actionFilePath = Path.Combine(_testActionsPath, "actions.json");
            Assert.IsTrue(File.Exists(actionFilePath));

            // 3. Manually modify the file
            var content = await File.ReadAllTextAsync(actionFilePath);
            var jsonDoc = JsonDocument.Parse(content);
            Assert.IsTrue(jsonDoc.RootElement.ValueKind == JsonValueKind.Object);

            // 4. Delete the file
            File.Delete(actionFilePath);
            Assert.IsFalse(File.Exists(actionFilePath));

            // 5. Try to load actions - should create new file
            var actions = await _actionService.LoadActionsAsync();
            Assert.IsNotNull(actions);
            Assert.AreEqual(0, actions.Count);
            Assert.IsTrue(File.Exists(actionFilePath));
        }
    }
}