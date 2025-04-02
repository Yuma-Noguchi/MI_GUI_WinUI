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
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.ViewModels.Base;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionStudioViewModel : ViewModelBase
    {
        private readonly IActionService _actionService;
        private XamlRoot? _xamlRoot;
        private bool _executingInference;

        [ObservableProperty]
        private ObservableCollection<ActionData> _actions;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActionSelected))]
        private ActionData? _selectedAction;

        [ObservableProperty]
        private string? _selectedButton;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private double _sleepDuration = 1.0;

        public bool IsActionSelected => SelectedAction != null;

        public XamlRoot? XamlRoot
        {
            get => _xamlRoot;
            set => _xamlRoot = value;
        }

        public ObservableCollection<string> AvailableButtons { get; } = new()
        {
            "A", "B", "X", "Y", "LB", "RB", "LT", "RT",
            "Start", "Back", "LS", "RS",
            "DPad_Up", "DPad_Down", "DPad_Left", "DPad_Right"
        };

        public ActionStudioViewModel(
            IActionService actionService,
            ILogger<ActionStudioViewModel> logger,
            INavigationService navigationService)
            : base(logger, navigationService)
        {
            _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
            _actions = new ObservableCollection<ActionData>();
        }

        protected override void OnWindowChanged()
        {
            base.OnWindowChanged();
            if (Window != null)
            {
                _xamlRoot = Window.Content?.XamlRoot;
            }
            else
            {
                _xamlRoot = null;
            }
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadActionsAsync();
        }

        private async Task LoadActionsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                IsLoading = true;
                ErrorMessage = null;

                var actions = await _actionService.LoadActionsAsync();
                Actions.Clear();
                foreach (var action in actions)
                {
                    Actions.Add(action);
                }
            }, nameof(LoadActionsAsync));
            IsLoading = false;
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
        private async Task AddToSequence()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrEmpty(SelectedButton) || SelectedAction == null)
                {
                    throw new InvalidOperationException("Please select a button and an action");
                }

                SelectedAction.Sequence.Add(SequenceItem.CreateButtonPress(SelectedButton));
            }, nameof(AddToSequence));
        }

        [RelayCommand]
        private async Task AddSleep()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (SelectedAction == null)
                {
                    throw new InvalidOperationException("No action selected");
                }

                if (SleepDuration <= 0)
                {
                    throw new InvalidOperationException("Sleep duration must be greater than 0");
                }

                SelectedAction.Sequence.Add(SequenceItem.CreateSleep(SleepDuration));
            }, nameof(AddSleep));
        }

        [RelayCommand]
        private async Task RemoveFromSequence(SequenceItem item)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (SelectedAction?.Sequence == null || item == null)
                {
                    throw new InvalidOperationException("No sequence item selected");
                }

                SelectedAction.Sequence.Remove(item);
            }, nameof(RemoveFromSequence));
        }

        [RelayCommand]
        private async Task SaveSequence()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (SelectedAction == null)
                {
                    throw new InvalidOperationException("No action selected");
                }

                if (string.IsNullOrWhiteSpace(SelectedAction.Name))
                {
                    throw new InvalidOperationException("Please enter an action name");
                }

                if (!SelectedAction.Sequence.Any())
                {
                    throw new InvalidOperationException("Please add at least one button to the sequence");
                }

                // Create a local copy of the selected action to prevent issues
                // if the selection changes during collection updates
                var actionToSave = SelectedAction;

                await _actionService.SaveActionAsync(actionToSave);
                
                // Update or add the action in the list
                var existingIndex = Actions.ToList().FindIndex(a => a.Name == actionToSave.Name);
                if (existingIndex >= 0)
                {
                    Actions[existingIndex] = actionToSave;
                    
                    // Ensure selection is maintained
                    if (SelectedAction == null)
                    {
                        SelectedAction = actionToSave;
                    }
                }
                else if (!Actions.Contains(actionToSave))
                {
                    Actions.Add(actionToSave);
                    
                    // Ensure selection is maintained
                    if (SelectedAction == null)
                    {
                        SelectedAction = actionToSave;
                    }
                }

                // Use the local copy for the dialog
                if (_xamlRoot != null)
                {
                    await DialogHelper.ShowMessage($"Action '{actionToSave.Name}' saved successfully.", "Success", _xamlRoot);
                }
            }, nameof(SaveSequence));
        }

        [RelayCommand]
        private async Task DeleteAction(ActionData action)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (action == null)
                {
                    throw new InvalidOperationException("No action selected for deletion");
                }

                if (_xamlRoot != null)
                {
                    var result = await DialogHelper.ShowConfirmation(
                        $"Are you sure you want to delete action '{action.Name}'?",
                        "Delete Action",
                        _xamlRoot
                    );

                    if (!result) return;
                }

                await _actionService.DeleteActionAsync(action.Id);
                Actions.Remove(action);

                if (SelectedAction == action)
                {
                    SelectedAction = null;
                }

                _logger.LogInformation($"Action deleted successfully: {action.Name}");
            }, nameof(DeleteAction));
        }

        public override void Cleanup()
        {
            try
            {
                Actions.Clear();
                SelectedAction = null;
                _xamlRoot = null;

                base.Cleanup();
                _logger.LogInformation("Cleaned up ActionStudioViewModel resources");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        protected override async Task ShowErrorAsync(string message)
        {
            ErrorMessage = message;
            if (_xamlRoot != null)
            {
                await DialogHelper.ShowError(message, _xamlRoot);
            }
        }
    }
}
