using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services.Interfaces;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class HeadTiltConfigurationViewModel : ViewModelBase
    {
        private PoseGuiElement _element = new();
        private Action<PoseGuiElement>? _onSave;

        [ObservableProperty]
        private bool isDialogOpen;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<EditorButton> availableButtons = new();

        [ObservableProperty]
        private EditorButton? selectedLeftSkin;

        [ObservableProperty]
        private EditorButton? selectedRightSkin;

        [ObservableProperty]
        private double sensitivity = 0.75;

        [ObservableProperty]
        private double deadzone = 1.0;

        [ObservableProperty]
        private bool linear = false;

        [ObservableProperty]
        private bool isEnabled;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        public HeadTiltConfigurationViewModel(
            ILogger<HeadTiltConfigurationViewModel> logger,
            INavigationService navigationService)
            : base(logger, navigationService)
        {
        }

        public void Configure(PoseGuiElement? element, IEnumerable<EditorButton> buttons, Action<PoseGuiElement> onSave)
        {
            try
            {
                _element = element ?? new PoseGuiElement();
                _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));

                // Update available buttons
                AvailableButtons.Clear();
                foreach (var button in buttons)
                {
                    AvailableButtons.Add(button);
                }

                // Load element settings
                IsEnabled = !string.IsNullOrEmpty(element?.File);
                Sensitivity = element?.Sensitivity ?? 0.75;
                Deadzone = element?.Deadzone ?? 1.0;
                Linear = element?.Linear ?? false;

                // Set selected skins
                SetupSkinSelection(element);

                ValidationMessage = string.Empty;
                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring head tilt");
                ValidationMessage = "Error loading configuration";
            }
        }

        private void SetupSkinSelection(PoseGuiElement? element)
        {
            try
            {
                // Default skins
                string leftSkinPath = "racing/left_arrow.png";
                string rightSkinPath = "racing/right_arrow.png";

                // Use element skins if available
                if (element != null)
                {
                    if (!string.IsNullOrEmpty(element.LeftSkin))
                        leftSkinPath = Utils.FileNameHelper.ConvertToAssetsRelativePath(element.LeftSkin);
                    if (!string.IsNullOrEmpty(element.RightSkin))
                        rightSkinPath = Utils.FileNameHelper.ConvertToAssetsRelativePath(element.RightSkin);
                }

                // Find matching buttons
                SelectedLeftSkin = AvailableButtons.FirstOrDefault(b => b.FileName == leftSkinPath);
                SelectedRightSkin = AvailableButtons.FirstOrDefault(b => b.FileName == rightSkinPath);

                // Fallback to first button if not found
                if (SelectedLeftSkin == null && AvailableButtons.Any())
                    SelectedLeftSkin = AvailableButtons.First();
                if (SelectedRightSkin == null && AvailableButtons.Any())
                    SelectedRightSkin = AvailableButtons.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up skin selection");
                ValidationMessage = "Error loading skins";
            }
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                string error = ValidateInputs();
                if (!string.IsNullOrEmpty(error))
                {
                    ValidationMessage = error;
                    return;
                }

                var updatedElement = new PoseGuiElement
                {
                    File = IsEnabled ? "head_tilt_joystick.py" : string.Empty,
                    LeftSkin = SelectedLeftSkin?.FileName ?? "racing/left_arrow.png",
                    RightSkin = SelectedRightSkin?.FileName ?? "racing/right_arrow.png",
                    Sensitivity = Sensitivity,
                    Deadzone = Deadzone,
                    Linear = Linear
                };

                _onSave?.Invoke(updatedElement);
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving head tilt configuration");
                ValidationMessage = "Error saving configuration";
            }
        }

        private string ValidateInputs()
        {
            if (!IsEnabled)
                return string.Empty;

            if (Sensitivity < 0.1 || Sensitivity > 1.0)
                return "Sensitivity must be between 0.1 and 1.0";

            if (Deadzone < 0.1 || Deadzone > 5.0)
                return "Deadzone must be between 0.1 and 5.0";

            if (SelectedLeftSkin == null)
                return "Please select a left tilt image";

            if (SelectedRightSkin == null)
                return "Please select a right tilt image";

            return string.Empty;
        }

        [RelayCommand]
        private void Cancel()
        {
            IsDialogOpen = false;
        }

        partial void OnValidationMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasValidationMessage));
        }

        public override void Cleanup()
        {
            try
            {
                AvailableButtons.Clear();
                _onSave = null;
                base.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }
    }
}