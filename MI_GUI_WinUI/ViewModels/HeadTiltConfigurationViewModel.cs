using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels.Base;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class HeadTiltConfigurationViewModel : ViewModelBase
    {
        private Action<PoseGuiElement>? _onSave;
        private PoseGuiElement? _currentElement;
        private const int MOTION_INPUT_WIDTH = 640;
        private const int MOTION_INPUT_HEIGHT = 480;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private double sensitivity = 0.75;

        [ObservableProperty]
        private double deadzone = 1.0;

        [ObservableProperty]
        private bool linear;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        public HeadTiltConfigurationViewModel(
            ILogger<HeadTiltConfigurationViewModel> logger,
            INavigationService navigationService) 
            : base(logger, navigationService)
        {
        }

        public void Configure(PoseGuiElement? element, Action<PoseGuiElement> onSave)
        {
            _onSave = onSave;

            // Determine enabled state based on file presence
            IsEnabled = element?.File == "head_tilt_joystick.py";

            // Copy existing settings or use defaults
            if (element != null)
            {
                // Create new instance with same values
                _currentElement = new PoseGuiElement
                {
                    File = element.File,
                    Position = element.Position ?? new List<int> { MOTION_INPUT_WIDTH / 2, MOTION_INPUT_HEIGHT / 2 },
                    Radius = element.Radius,
                    Skin = element.Skin,
                    LeftSkin = element.LeftSkin ?? "racing/left_arrow.png",
                    RightSkin = element.RightSkin ?? "racing/right_arrow.png",
                    Sensitivity = IsEnabled ? element.Sensitivity : 0.75,
                    Deadzone = IsEnabled ? element.Deadzone : 1.0,
                    Linear = IsEnabled && element.Linear,
                    Landmark = element.Landmark,
                    Action = element.Action
                };

                // Update observable properties
                Sensitivity = IsEnabled ? element.Sensitivity : 0.75;
                Deadzone = IsEnabled ? element.Deadzone : 1.0;
                Linear = IsEnabled && element.Linear;
            }
            else
            {
                // Initialize with defaults
                _currentElement = new PoseGuiElement
                {
                    Position = new List<int> { MOTION_INPUT_WIDTH / 2, MOTION_INPUT_HEIGHT / 2 },
                    LeftSkin = "racing/left_arrow.png",
                    RightSkin = "racing/right_arrow.png"
                };

                // Set default values for controls
                IsEnabled = false;
                Sensitivity = 0.75;
                Deadzone = 1.0;
                Linear = false;
            }

            ValidationMessage = string.Empty;
        }

        [RelayCommand]
        public void Save()
        {
            try
            {
                if (!IsEnabled)
                {
                    if (_currentElement?.File == "head_tilt_joystick.py")
                    {
                        // Remove head tilt configuration but preserve position
                        var emptyElement = new PoseGuiElement();
                        if (_currentElement?.Position != null)
                        {
                            emptyElement.Position = new List<int>(_currentElement.Position);
                        }
                        _onSave?.Invoke(emptyElement);
                    }
                    ValidationMessage = string.Empty;
                    return;
                }

                // Validate settings
                if (Sensitivity < 0.1 || Sensitivity > 1.0)
                {
                    ValidationMessage = "Sensitivity must be between 0.1 and 1.0";
                    return;
                }

                if (Deadzone < 0.1 || Deadzone > 5.0)
                {
                    ValidationMessage = "Deadzone must be between 0.1 and 5.0";
                    return;
                }

                // Create or update head tilt element
                var headTiltElement = new PoseGuiElement
                {
                    File = "head_tilt_joystick.py",
                    LeftSkin = "racing/left_arrow.png",
                    RightSkin = "racing/right_arrow.png",
                    Sensitivity = Sensitivity,
                    Deadzone = Deadzone,
                    Linear = Linear,
                    Position = _currentElement?.Position != null ? 
                        new List<int>(_currentElement.Position) : 
                        new List<int> { MOTION_INPUT_WIDTH / 2, MOTION_INPUT_HEIGHT / 2 }
                };

                _onSave?.Invoke(headTiltElement);
                ValidationMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving head tilt configuration");
                ValidationMessage = "Error saving configuration: " + ex.Message;
            }
        }

        public override void Cleanup()
        {
            _onSave = null;
            _currentElement = null;
            ValidationMessage = string.Empty;
            base.Cleanup();
        }

        partial void OnValidationMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasValidationMessage));
        }
    }
}