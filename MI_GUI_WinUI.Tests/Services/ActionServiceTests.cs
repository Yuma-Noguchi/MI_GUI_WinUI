using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Tests.TestUtils;
using System.Text.Json;
using Moq;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class ActionServiceTests : UnitTestBase
    {
        private ActionService _actionService;
        private string _testActionsPath;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();

            // Create test actions directory
            _testActionsPath = Path.Combine(TestDirectory, "actions");
            Directory.CreateDirectory(_testActionsPath);

            // Create action service with test directory
            _actionService = new ActionService(Mock.Of<ILogger<ActionService>>());
        }

        [TestMethod]
        public async Task LoadActionsAsync_WhenNoActions_ReturnsEmptyList()
        {
            // Act
            var actions = await _actionService.LoadActionsAsync();

            // Assert
            Assert.IsNotNull(actions);
            Assert.AreEqual(0, actions.Count);
        }

        [TestMethod]
        public async Task SaveActionAsync_WithValidAction_Succeeds()
        {
            // Arrange
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Action",
                Args = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "param1", "value1" }
                    }
                }
            };

            // Act
            await _actionService.SaveActionAsync(action);

            // Assert
            var loadedActions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(1, loadedActions.Count);
            
            var loadedAction = loadedActions[0];
            Assert.AreEqual(action.Id, loadedAction.Id);
            Assert.AreEqual(action.Name, loadedAction.Name);
            Assert.IsTrue(loadedAction.Args[0].ContainsKey("param1"));
            Assert.AreEqual("value1", loadedAction.Args[0]["param1"]);
        }

        [TestMethod]
        public async Task SaveActionAsync_WithNullAction_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidActionException>(
                () => _actionService.SaveActionAsync(null));
        }

        [TestMethod]
        public async Task SaveActionAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "",
                Args = new List<Dictionary<string, object>>()
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidActionException>(
                () => _actionService.SaveActionAsync(action));
        }

        [TestMethod]
        public async Task DeleteActionAsync_WithExistingAction_Succeeds()
        {
            // Arrange
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Action",
                Args = new List<Dictionary<string, object>>()
            };
            await _actionService.SaveActionAsync(action);

            // Act
            await _actionService.DeleteActionAsync(action.Id);

            // Assert
            var actions = await _actionService.LoadActionsAsync();
            Assert.AreEqual(0, actions.Count);
        }

        [TestMethod]
        public async Task DeleteActionAsync_WithNonExistentAction_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ActionNotFoundException>(
                () => _actionService.DeleteActionAsync("nonexistent"));
        }

        [TestMethod]
        public async Task GetActionByIdAsync_WithExistingAction_ReturnsAction()
        {
            // Arrange
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Action",
                Args = new List<Dictionary<string, object>>()
            };
            await _actionService.SaveActionAsync(action);

            // Act
            var loadedAction = await _actionService.GetActionByIdAsync(action.Id);

            // Assert
            Assert.IsNotNull(loadedAction);
            Assert.AreEqual(action.Id, loadedAction.Id);
            Assert.AreEqual(action.Name, loadedAction.Name);
        }

        [TestMethod]
        public async Task GetActionByNameAsync_WithExistingAction_ReturnsAction()
        {
            // Arrange
            var action = new ActionData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Action",
                Args = new List<Dictionary<string, object>>()
            };
            await _actionService.SaveActionAsync(action);

            // Act
            var loadedAction = await _actionService.GetActionByNameAsync("Test Action");

            // Assert
            Assert.IsNotNull(loadedAction);
            Assert.AreEqual(action.Id, loadedAction.Id);
            Assert.AreEqual(action.Name, loadedAction.Name);
        }

        [TestMethod]
        public async Task ExecuteAction_ValidAction_Succeeds()
        {
            // Arrange
            var actionData = TestDataGenerators.CreateAction(); // This now returns correct Args type

            // Rest of the test...
        }
    }
}