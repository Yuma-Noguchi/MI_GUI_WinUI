using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Utils;
using Microsoft.UI.Xaml;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionStudioViewModel : ObservableObject
    {
        private readonly ILogger<ActionStudioViewModel> _logger;
        private readonly string ACTIONS_DIR;

        [ObservableProperty]
        private ObservableCollection<Models.Action> _actions;

        [ObservableProperty]
        private Models.Action? _selectedAction;

        [ObservableProperty]
        private string? _selectedButton;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private XamlRoot? _xamlRoot;

        public bool IsActionSelected => SelectedAction != null;

        public ObservableCollection<string> AvailableButtons { get; } = new()
        {
            "A", "B", "X", "Y", "LB", "RB", "LT", "RT",
            "Start", "Back", "LS", "RS",
            "DPad_Up", "DPad_Down", "DPad_Left", "DPad_Right"
        };

        public ObservableCollection<Dictionary<string, string[]>> ActionSequence { get; } = new();

        public ActionStudioViewModel(ILogger<ActionStudioViewModel> logger)
        {
            _logger = logger;
            _actions = new ObservableCollection<Models.Action>();
            ACTIONS_DIR = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "MotionInput", "data", "assets", "generated_actions"
            );

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                if (!Directory.Exists(ACTIONS_DIR))
                {
                    Directory.CreateDirectory(ACTIONS_DIR);
                    return;
                }

                var files = Directory.GetFiles(ACTIONS_DIR, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        Actions.Add(new Models.Action { Name = name });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading action file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing actions directory");
                ErrorMessage = "Failed to load actions. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CreateAction()
        {
            var newAction = new Models.Action
            {
                Name = $"New Action {_actions.Count + 1}"
            };
            
            Actions.Add(newAction);
            SelectedAction = newAction;
            ActionSequence.Clear();
            ErrorMessage = null;
        }

        [RelayCommand]
        private void AddToSequence()
        {
            if (string.IsNullOrEmpty(SelectedButton) || SelectedAction == null) 
                return;

            ActionSequence.Add(new Dictionary<string, string[]>
            {
                { "press", new[] { SelectedButton.ToLower() } }
            });
            ErrorMessage = null;
        }

        [RelayCommand]
        private void RemoveFromSequence(Dictionary<string, string[]> sequence)
        {
            if (sequence != null)
            {
                ActionSequence.Remove(sequence);
                ErrorMessage = null;
            }
        }

        [RelayCommand]
        private async Task SaveSequence()
        {
            if (SelectedAction == null || string.IsNullOrWhiteSpace(SelectedAction.Name))
            {
                ErrorMessage = "Please enter an action name";
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowError("Please enter an action name.", XamlRoot);
                }
                return;
            }

            if (!ActionSequence.Any())
            {
                ErrorMessage = "Please add at least one button to the sequence";
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowError("Please add at least one button to the sequence.", XamlRoot);
                }
                return;
            }

            try
            {
                var actionData = new
                {
                    action = new
                    {
                        @class = "ds4_gamepad",
                        method = "chain",
                        args = ActionSequence.ToList()
                    }
                };

                // Use sanitized name for file
                var sanitizedName = SelectedAction.Name.Replace(" ", "_");
                var filePath = Path.Combine(ACTIONS_DIR, $"{sanitizedName}.json");
                var json = JsonConvert.SerializeObject(actionData, Formatting.Indented);
                
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInformation($"Action saved successfully: {filePath}");

                // Update action list if this is a new action
                if (!Actions.Any(a => a.Name == SelectedAction.Name))
                {
                    Actions.Add(SelectedAction);
                }

                ErrorMessage = null;
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowMessage($"Action '{SelectedAction.Name}' saved successfully.", "Success", XamlRoot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving action: {SelectedAction.Name}");
                ErrorMessage = "Failed to save action. Please try again.";
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowError("Failed to save action. Please try again.", XamlRoot);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteAction(Models.Action action)
        {
            if (action == null) return;

            try
            {
                if (XamlRoot != null)
                {
                    var result = await DialogHelper.ShowConfirmation(
                        $"Are you sure you want to delete action '{action.Name}'?",
                        "Delete Action",
                        XamlRoot
                    );

                    if (!result) return;
                }

                var filePath = Path.Combine(ACTIONS_DIR, $"{action.Name}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                Actions.Remove(action);
                if (SelectedAction == action)
                {
                    SelectedAction = null;
                    ActionSequence.Clear();
                }

                _logger.LogInformation($"Action deleted successfully: {action.Name}");
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting action: {action.Name}");
                ErrorMessage = "Failed to delete action. Please try again.";
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowError("Failed to delete action. Please try again.", XamlRoot);
                }
            }
        }

        partial void OnSelectedActionChanged(Models.Action? value)
        {
            OnPropertyChanged(nameof(IsActionSelected));
            
            // Clear current sequence
            ActionSequence.Clear();
            ErrorMessage = null;

            // Load sequence if an action is selected
            if (value != null)
            {
                LoadActionSequence(value.Name);
            }
        }

        private async void LoadActionSequence(string actionName)
        {
            try
            {
                IsLoading = true;
                var filePath = Path.Combine(ACTIONS_DIR, $"{actionName}.json");
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Action file not found: {filePath}");
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonConvert.DeserializeObject<dynamic>(json);

                if (data?.action?.args != null)
                {
                    var args = data.action.args;
                    foreach (var arg in args)
                    {
                        if (arg is Dictionary<string, string[]> sequence)
                        {
                            ActionSequence.Add(sequence);
                        }
                    }
                }

                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading action sequence: {actionName}");
                ErrorMessage = "Failed to load action sequence.";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
