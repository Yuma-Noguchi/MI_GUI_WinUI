using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Models;
using Newtonsoft.Json;
using System.Linq;

namespace MI_GUI_WinUI.Services
{
    public class ActionService
    {
        private readonly ILogger<ActionService> _logger;
        private readonly string ACTIONS_FILE;
        private Dictionary<string, ActionData> _actions = new();

        public ActionService(ILogger<ActionService> logger)
        {
            _logger = logger;
            ACTIONS_FILE = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "MotionInput", "data", "actions", "actions.json"
            );
            EnsureActionsFolderExists();
        }

        private void EnsureActionsFolderExists()
        {
            var directory = Path.GetDirectoryName(ACTIONS_FILE);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task<List<ActionData>> LoadActionsAsync()
        {
            try
            {
                if (!File.Exists(ACTIONS_FILE))
                {
                    _logger.LogInformation("Actions file not found, creating new one");
                    await SaveActionsToFileAsync();
                    return new List<ActionData>();
                }

                var json = await File.ReadAllTextAsync(ACTIONS_FILE);
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };

                _actions = JsonConvert.DeserializeObject<Dictionary<string, ActionData>>(json, settings) 
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
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };

                var json = JsonConvert.SerializeObject(_actions, settings);
                await File.WriteAllTextAsync(ACTIONS_FILE, json);
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
                throw new InvalidActionException("Action cannot be null");
            }

            if (string.IsNullOrWhiteSpace(action.Name))
            {
                throw new InvalidActionException("Action name cannot be empty");
            }

            if (action.Args == null)
            {
                throw new InvalidActionException("Action arguments cannot be null");
            }
        }

        public async Task SaveActionAsync(ActionData action)
        {
            ValidateAction(action);

            // Check if an action with this name already exists
            var existingAction = _actions.Values.FirstOrDefault(a => a.Name == action.Name);
            if (existingAction != null)
            {
                // Update the existing action's sequence
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
            if (_actions.ContainsKey(actionId))
            {
                var actionName = _actions[actionId].Name;
                _actions.Remove(actionId);
                await SaveActionsToFileAsync();
                _logger.LogInformation($"Action deleted: {actionName}");
            }
            else
            {
                throw new ActionNotFoundException($"Action with ID {actionId} not found");
            }
        }

        public async Task<ActionData> GetActionByIdAsync(string id)
        {
            if (_actions.TryGetValue(id, out var action))
            {
                return action;
            }
            throw new ActionNotFoundException($"Action with ID {id} not found");
        }

        public async Task<ActionData> GetActionByNameAsync(string name)
        {
            var action = _actions.Values.FirstOrDefault(a => a.Name == name);
            if (action != null)
            {
                return action;
            }
            throw new ActionNotFoundException($"Action with name '{name}' not found");
        }

        public void ClearCache()
        {
            _actions.Clear();
        }
    }
}
