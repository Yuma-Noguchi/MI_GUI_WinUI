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
        private Dictionary<string, string> _nameToIdMap = new();

        private class ActionFileData
        {
            [JsonProperty("actions")]
            public Dictionary<string, ActionData> Actions { get; set; } = new();

            [JsonProperty("metadata")]
            public ActionMetadata Metadata { get; set; } = new();
        }

        private class ActionMetadata
        {
            [JsonProperty("version")]
            public string Version { get; set; } = "2.0";

            [JsonProperty("nameToId")]
            public Dictionary<string, string> NameToId { get; set; } = new();
        }

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

        private async Task<ActionFileData> LoadFileDataAsync()
        {
            if (!File.Exists(ACTIONS_FILE))
            {
                _logger.LogInformation("Actions file not found, creating new one");
                var fileData = new ActionFileData();
                await SaveActionsToFileAsync(fileData);
                return fileData;
            }

            var json = await File.ReadAllTextAsync(ACTIONS_FILE);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            try
            {
                // Try loading new format first
                var fileData = JsonConvert.DeserializeObject<ActionFileData>(json, settings);
                if (fileData?.Actions != null)
                {
                    return fileData;
                }
            }
            catch
            {
                _logger.LogInformation("Failed to load new format, attempting migration from old format");
            }

            // Try migrating from old format
            return await MigrateFromOldFormatAsync(json);
        }

        private async Task<ActionFileData> MigrateFromOldFormatAsync(string json)
        {
            try
            {
                var oldData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
                var fileData = new ActionFileData();

                foreach (var kvp in oldData)
                {
                    var action = ActionData.FromLegacyFormat(kvp.Value);
                    fileData.Actions[action.Id] = action;
                    fileData.Metadata.NameToId[action.Name] = action.Id;
                }

                // Save migrated data
                await SaveActionsToFileAsync(fileData);
                _logger.LogInformation("Successfully migrated from old format");

                return fileData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating from old format");
                throw new InvalidActionException("Failed to migrate from old format: " + ex.Message);
            }
        }

        public async Task<List<ActionData>> LoadActionsAsync()
        {
            try
            {
                var fileData = await LoadFileDataAsync();
                _actions = fileData.Actions;
                _nameToIdMap = fileData.Metadata.NameToId;

                return _actions.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading actions");
                throw;
            }
        }

        private async Task SaveActionsToFileAsync(ActionFileData fileData = null)
        {
            try
            {
                fileData ??= new ActionFileData
                {
                    Actions = _actions,
                    Metadata = new ActionMetadata
                    {
                        NameToId = _nameToIdMap
                    }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };

                var json = JsonConvert.SerializeObject(fileData, settings);
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

            if (string.IsNullOrWhiteSpace(action.Id))
            {
                throw new InvalidActionException("Action ID cannot be empty");
            }

            if (action.Args == null)
            {
                throw new InvalidActionException("Action arguments cannot be null");
            }
        }

        public async Task<bool> IsNameAvailable(string name, string currentActionId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (!_nameToIdMap.TryGetValue(name, out var existingId))
            {
                return true;
            }

            // If checking for an existing action's current name
            return currentActionId != null && existingId == currentActionId;
        }

        public async Task SaveActionAsync(ActionData action)
        {
            ValidateAction(action);

            // For new actions or renamed actions
            if (_actions.TryGetValue(action.Id, out var existingAction))
            {
                // If trying to rename to a name that's already taken
                if (existingAction.Name != action.Name && !await IsNameAvailable(action.Name, action.Id))
                {
                    throw new ActionNameExistsException(action.Name);
                }

                // Remove the old name mapping if it's being renamed
                if (existingAction.Name != action.Name)
                {
                    _nameToIdMap.Remove(existingAction.Name);
                    _logger.LogInformation($"Renamed action from {existingAction.Name} to {action.Name}");
                }
            }
            else
            {
                // For completely new actions
                if (!await IsNameAvailable(action.Name))
                {
                    throw new ActionNameExistsException(action.Name);
                }
            }

            // Update both dictionaries
            _actions[action.Id] = action;
            _nameToIdMap[action.Name] = action.Id;

            await SaveActionsToFileAsync();
            _logger.LogInformation($"Action saved: {action.Name} with ID: {action.Id}");
        }

        public async Task DeleteActionAsync(string actionName)
        {
            if (_nameToIdMap.TryGetValue(actionName, out var id))
            {
                _actions.Remove(id);
                _nameToIdMap.Remove(actionName);
                await SaveActionsToFileAsync();
                _logger.LogInformation($"Action deleted: {actionName}");
            }
            else
            {
                throw new ActionNotFoundException(actionName);
            }
        }

        public void ClearCache()
        {
            _actions.Clear();
            _nameToIdMap.Clear();
        }
    }
}
