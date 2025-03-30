using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Utils;
using System.Collections.Generic;
using MI_GUI_WinUI.Services;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionStudioViewModel : ObservableObject
    {
        private readonly ILogger<ActionStudioViewModel> _logger;
        private readonly ActionService _actionService;

        [ObservableProperty]
        private ObservableCollection<ActionData> _actions;

        [ObservableProperty]
        private ActionData? _selectedAction;

        [ObservableProperty]
        private string? _selectedButton;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private double _sleepDuration = 1.0;

        [ObservableProperty]
        private XamlRoot? xamlRoot;

        public bool IsActionSelected => SelectedAction != null;

        public ObservableCollection<string> AvailableButtons { get; } = new()
        {
            "A", "B", "X", "Y", "LB", "RB", "LT", "RT",
            "Start", "Back", "LS", "RS",
            "DPad_Up", "DPad_Down", "DPad_Left", "DPad_Right"
        };

        public ActionStudioViewModel(ILogger<ActionStudioViewModel> logger, ActionService actionService)
        {
            _logger = logger;
            _actionService = actionService;
            _actions = new ObservableCollection<ActionData>();

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var actions = await _actionService.LoadActionsAsync();
                Actions.Clear();
                foreach (var action in actions)
                {
                    Actions.Add(action);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing actions");
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
            var newAction = new ActionData
            {
                Name = $"New Action {Actions.Count + 1}"
            };
            
            Actions.Add(newAction);
            SelectedAction = newAction;
            ErrorMessage = null;
        }

        [RelayCommand]
        private void AddToSequence()
        {
            if (string.IsNullOrEmpty(SelectedButton) || SelectedAction == null) 
                return;

            SelectedAction.Sequence.Add(SequenceItem.CreateButtonPress(SelectedButton));
            ErrorMessage = null;
        }

        [RelayCommand]
        private void AddSleep()
        {
            if (SelectedAction == null) return;

            SelectedAction.Sequence.Add(SequenceItem.CreateSleep(SleepDuration));
            ErrorMessage = null;
        }

        [RelayCommand]
        private void RemoveFromSequence(SequenceItem item)
        {
            if (SelectedAction?.Sequence == null || item == null) 
                return;

            SelectedAction.Sequence.Remove(item);
            ErrorMessage = null;
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

            if (!SelectedAction.Sequence.Any())
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
                await _actionService.SaveActionAsync(SelectedAction);

                ErrorMessage = null;
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowMessage($"Action '{SelectedAction.Name}' saved successfully.", "Success", XamlRoot);
                }
            }
            catch (ActionNameExistsException)
            {
                ErrorMessage = "An action with this name already exists";
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowError("An action with this name already exists. Please choose a different name.", XamlRoot);
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
        private async Task DeleteAction(ActionData action)
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

                await _actionService.DeleteActionAsync(action.Name);
                Actions.Remove(action);

                if (SelectedAction == action)
                {
                    SelectedAction = null;
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

        partial void OnSelectedActionChanged(ActionData? value)
        {
            OnPropertyChanged(nameof(IsActionSelected));
        }
    }
}
