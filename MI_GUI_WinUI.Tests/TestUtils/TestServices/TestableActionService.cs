using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.TestUtils.TestServices
{
    /// <summary>
    /// A testable version of ActionService that doesn't rely on Windows.ApplicationModel.Package.Current
    /// </summary>
    public class TestableActionService : IActionService
    {
        private readonly ILogger<ActionService> _logger;
        private string _actionsFile;
        private Dictionary<string, ActionData> _actions = new();
        private readonly JsonSerializerSettings _jsonSettings;

        public TestableActionService(ILogger<ActionService> logger, string actionsFilePath = null)
        {
            _logger = logger;
            _actionsFile = actionsFilePath ?? Path.Combine(Path.GetTempPath(), "test_actions.json");
            _jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            EnsureActionsFolderExists();
        }

        private void EnsureActionsFolderExists()
        {
            var directory = Path.GetDirectoryName(_actionsFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task<List<ActionData>> LoadActionsAsync()
        {
            try
            {
                if (!File.Exists(_actionsFile))
                {
                    _logger.LogInformation("Actions file not found, creating new one");
                    await SaveActionsToFileAsync();
                    return new List<ActionData>();
                }

                var json = await File.ReadAllTextAsync(_actionsFile);
                
                _actions = JsonConvert.DeserializeObject<Dictionary<string, ActionData>>(json, _jsonSettings) 
                    ?? new Dictionary<string, ActionData>();

                return _actions.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading actions");
                throw;
            }
        }

        private async Task SaveActionsToFileAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_actions, _jsonSettings);
                await File.WriteAllTextAsync(_actionsFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving actions to file");
                throw;
            }
        }

        private void ValidateAction(ActionData action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Action cannot be null");
            }

            if (string.IsNullOrWhiteSpace(action.Name))
            {
                throw new ArgumentException("Action name cannot be empty", nameof(action));
            }

            if (action.Args == null)
            {
                throw new ArgumentException("Action arguments cannot be null", nameof(action));
            }

            // Ensure we have an ID
            if (string.IsNullOrEmpty(action.Id))
            {
                action.Id = Guid.NewGuid().ToString();
            }
        }

        public async Task SaveActionAsync(ActionData action)
        {
            ValidateAction(action);

            // Check if an action with this name already exists
            var existingAction = _actions.Values.FirstOrDefault(a => a.Name == action.Name);
            if (existingAction != null)
            {
                // Update the existing action's arguments
                existingAction.Args = action.Args;
                action.Id = existingAction.Id; // Make sure we're using the existing ID
            }

            // Save/Update the action using its ID
            _actions[action.Id] = action;
            await SaveActionsToFileAsync();

            _logger.LogInformation($"Action saved: {action.Name} with ID: {action.Id}");
        }

        public async Task DeleteActionAsync(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                throw new ArgumentException("Action ID cannot be empty", nameof(actionId));
            }

            if (_actions.ContainsKey(actionId))
            {
                var actionName = _actions[actionId].Name;
                _actions.Remove(actionId);
                await SaveActionsToFileAsync();
                _logger.LogInformation($"Action deleted: {actionName}");
            }
            else
            {
                throw new KeyNotFoundException($"Action with ID {actionId} not found");
            }
        }

        public Task<ActionData> GetActionByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Action ID cannot be empty", nameof(id));
            }

            if (_actions.TryGetValue(id, out var action))
            {
                return Task.FromResult(action);
            }
            throw new KeyNotFoundException($"Action with ID {id} not found");
        }

        public Task<ActionData> GetActionByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Action name cannot be empty", nameof(name));
            }

            var action = _actions.Values.FirstOrDefault(a => a.Name == name);
            if (action != null)
            {
                return Task.FromResult(action);
            }
            throw new KeyNotFoundException($"Action with name '{name}' not found");
        }

        public void ClearCache()
        {
            _actions.Clear();
        }

        // For testing purposes
        public void SetActionsFilePath(string path)
        {
            _actionsFile = path;
            EnsureActionsFolderExists();
        }
    }
}