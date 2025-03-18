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
        private Dictionary<string, ActionData> _actionsCache = new();

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

                var result = JsonConvert.DeserializeObject<Dictionary<string, ActionData>>(json, settings);
                _actionsCache = result ?? new Dictionary<string, ActionData>();

                return _actionsCache.Values.ToList();
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

                var json = JsonConvert.SerializeObject(_actionsCache, settings);
                await File.WriteAllTextAsync(ACTIONS_FILE, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving actions to file");
                throw;
            }
        }

        public async Task SaveActionAsync(ActionData action)
        {
            if (string.IsNullOrWhiteSpace(action.Name))
            {
                throw new ArgumentException("Action name cannot be empty");
            }

            _actionsCache[action.Name] = action;
            await SaveActionsToFileAsync();
            _logger.LogInformation($"Action saved: {action.Name}");
        }

        public async Task DeleteActionAsync(string actionName)
        {
            if (_actionsCache.ContainsKey(actionName))
            {
                _actionsCache.Remove(actionName);
                await SaveActionsToFileAsync();
                _logger.LogInformation($"Action deleted: {actionName}");
            }
        }

        public void ClearCache()
        {
            _actionsCache.Clear();
        }
    }
}
