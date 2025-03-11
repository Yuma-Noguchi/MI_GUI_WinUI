using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows.Input;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionConfigurationDialogViewModel : ObservableObject
    {
        private Action<UnifiedGuiElement>? _onSave;
        private UnifiedGuiElement _element;
        
        [ObservableProperty]
        private bool isDialogOpen;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        [ObservableProperty]
        private ObservableCollection<MethodDescription> availableMethods;

        [ObservableProperty]
        private string selectedClass;

        [ObservableProperty]
        private MethodDescription selectedMethod;

        [ObservableProperty]
        private ObservableCollection<ArgumentInfo> argumentsWithDescriptions = new();

        // Pose detection properties
        [ObservableProperty]
        private bool isPoseEnabled;

        [ObservableProperty]
        private double sensitivity = 1.0;

        [ObservableProperty]
        private int deadzone = 10;

        [ObservableProperty]
        private bool linear = true;

        [ObservableProperty]
        private bool hasLeftWrist;

        [ObservableProperty]
        private bool hasRightWrist;

        [ObservableProperty]
        private bool hasLeftElbow;

        [ObservableProperty]
        private bool hasRightElbow;

        [ObservableProperty]
        private bool hasLeftShoulder;

        [ObservableProperty]
        private bool hasRightShoulder;

        public ActionConfigurationDialogViewModel()
        {
            AvailableMethods = new ObservableCollection<MethodDescription>
            {
                new("button_down", "Hold Button"),
                new("button_up", "Release Button"),
                new("hold_button", "Hold Button for Duration"),
                new("press_button", "Press Button Multiple Times"),
                new("toggle_button", "Toggle Button On/Off"),
                new("left_joystick", "Move Left Joystick"),
                new("right_joystick", "Move Right Joystick"),
                new("left_trigger", "Set Left Trigger"),
                new("right_trigger", "Set Right Trigger")
            };

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            SelectedClass = "ds4_gamepad";
            ArgumentsWithDescriptions = new ObservableCollection<ArgumentInfo>();
            SelectedMethod = AvailableMethods[0];
            UpdateArgumentInputs();
        }

        public string HeaderText => IsPoseEnabled ? "Pose Detection Help" : "Available Buttons";

        public string HelpText => IsPoseEnabled ? 
            "• Sensitivity affects how quickly the pose is detected\n" +
            "• Deadzone sets minimum movement required\n" +
            "• Linear movement provides smoother transitions\n" +
            "• Select landmarks that should trigger the action" :
            "A, B, X, Y\n" +
            "LB, RB, LT, RT\n" +
            "Start, Back, LS, RS\n" +
            "DPad_Up, DPad_Down, DPad_Left, DPad_Right";

        public void Initialize(UnifiedGuiElement element, Action<UnifiedGuiElement> onSave)
        {
            _element = element;
            _onSave = onSave;

            SelectedClass = !string.IsNullOrEmpty(element.Action.ClassName) ? element.Action.ClassName : "ds4_gamepad";
            
            if (!string.IsNullOrEmpty(element.Action.MethodName))
            {
                var method = AvailableMethods.FirstOrDefault(m => m.Id == element.Action.MethodName);
                if (method != null)
                {
                    SelectedMethod = method;
                    ArgumentsWithDescriptions.Clear();
                    if (element.Action.Arguments?.Any() == true)
                    {
                        var descriptions = GetArgumentDescriptions(method.Id);
                        for (int i = 0; i < element.Action.Arguments.Count; i++)
                        {
                            string desc = i < descriptions.Length ? descriptions[i] : $"Argument {i + 1}";
                            ArgumentsWithDescriptions.Add(new ArgumentInfo(desc, element.Action.Arguments[i]));
                        }
                    }
                    else
                    {
                        UpdateArgumentInputs();
                    }
                }
            }
            else
            {
                SelectedMethod = AvailableMethods.First();
                UpdateArgumentInputs();
            }

            // Set up pose settings
            IsPoseEnabled = element.IsPose;
            if (element.IsPose)
            {
                Sensitivity = element.Sensitivity ?? 1.0;
                Deadzone = element.Deadzone ?? 10;
                Linear = element.Linear ?? true;

                // Set landmark checkboxes
                HasLeftWrist = element.Landmarks.Contains("LEFT_WRIST");
                HasRightWrist = element.Landmarks.Contains("RIGHT_WRIST");
                HasLeftElbow = element.Landmarks.Contains("LEFT_ELBOW");
                HasRightElbow = element.Landmarks.Contains("RIGHT_ELBOW");
                HasLeftShoulder = element.Landmarks.Contains("LEFT_SHOULDER");
                HasRightShoulder = element.Landmarks.Contains("RIGHT_SHOULDER");
            }
            
            ValidationMessage = string.Empty;
            IsDialogOpen = true;
        }

        private string[] GetArgumentDescriptions(string methodId) => methodId switch
        {
            "button_down" => new[] { "Button to Hold Down" },
            "button_up" => new[] { "Button to Release" },
            "toggle_button" => new[] { "Button to Toggle" },
            "hold_button" => new[] { "Button to Hold", "Duration in Seconds (e.g., 0.5)" },
            "press_button" => new[] { "Button to Press", "Number of Times to Press (e.g., 2)" },
            "left_joystick" or "right_joystick" => new[] { "X Position (-1.0 to 1.0)", "Y Position (-1.0 to 1.0)" },
            "left_trigger" or "right_trigger" => new[] { "Trigger Pressure (0.0 to 1.0)" },
            _ => Array.Empty<string>()
        };

        partial void OnSelectedMethodChanged(MethodDescription value)
        {
            UpdateArgumentInputs();
            ValidationMessage = string.Empty;
        }

        partial void OnIsPoseEnabledChanged(bool value)
        {
            OnPropertyChanged(nameof(HeaderText));
            
            // Update file and landmarks when switching between pose and regular modes
            if (value)
            {
                _element = _element with { File = "hit_trigger.py" };
                UpdateLandmarks();
            }
            else
            {
                _element = _element with
                { 
                    File = "button",
                    Landmarks = new List<string>(),
                    LeftSkin = null,
                    RightSkin = null,
                    Sensitivity = null,
                    Deadzone = null,
                    Linear = null
                };
            }
        }

        private void UpdateLandmarks()
        {
            var landmarks = new List<string>();
            if (HasLeftWrist) landmarks.Add("LEFT_WRIST");
            if (HasRightWrist) landmarks.Add("RIGHT_WRIST");
            if (HasLeftElbow) landmarks.Add("LEFT_ELBOW");
            if (HasRightElbow) landmarks.Add("RIGHT_ELBOW");
            if (HasLeftShoulder) landmarks.Add("LEFT_SHOULDER");
            if (HasRightShoulder) landmarks.Add("RIGHT_SHOULDER");

            _element = _element.WithLandmarks(landmarks);
        }

        private void UpdateArgumentInputs()
        {
            ArgumentsWithDescriptions.Clear();
            var descriptions = GetArgumentDescriptions(SelectedMethod.Id);

            switch (SelectedMethod.Id)
            {
                case "button_down":
                case "button_up":
                case "toggle_button":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "A"));
                    break;

                case "hold_button":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "A"));
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[1], "0.5"));
                    break;

                case "press_button":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "A"));
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[1], "2"));
                    break;

                case "left_joystick":
                case "right_joystick":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "0.0"));
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[1], "0.0"));
                    break;

                case "left_trigger":
                case "right_trigger":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "0.5"));
                    break;
            }
        }

        private string ValidateInputs()
        {
            try
            {
                var arguments = ArgumentsWithDescriptions.Select(a => a.Value).ToList();

                // Validate action configuration
                switch (SelectedMethod.Id)
                {
                    case "button_down":
                    case "button_up":
                    case "toggle_button":
                    case "press_button":
                    case "hold_button":
                        if (string.IsNullOrWhiteSpace(arguments[0]))
                            return "Please enter a button name";
                            
                        if (arguments.Count > 1)
                        {
                            if (!float.TryParse(arguments[1], out float paramValue))
                                return "Please enter a valid number";
                                
                            if (SelectedMethod.Id == "hold_button" && paramValue <= 0)
                                return "Duration must be greater than 0";
                                
                            if (SelectedMethod.Id == "press_button" && paramValue < 1)
                                return "Must press at least once";
                        }
                        break;

                    case "left_joystick":
                    case "right_joystick":
                        if (!float.TryParse(arguments[0], out float xAxis) || xAxis < -1 || xAxis > 1)
                            return "X-axis must be between -1.0 and 1.0";
                            
                        if (!float.TryParse(arguments[1], out float yAxis) || yAxis < -1 || yAxis > 1)
                            return "Y-axis must be between -1.0 and 1.0";
                        break;

                    case "left_trigger":
                    case "right_trigger":
                        if (!float.TryParse(arguments[0], out float triggerValue) || triggerValue < 0 || triggerValue > 1)
                            return "Trigger value must be between 0.0 and 1.0";
                        break;
                }

                // Validate pose settings if enabled
                if (IsPoseEnabled)
                {
                    if (Sensitivity < 0.1 || Sensitivity > 2.0)
                        return "Sensitivity must be between 0.1 and 2.0";

                    if (Deadzone < 0 || Deadzone > 50)
                        return "Deadzone must be between 0 and 50";

                    var selectedLandmarks = new[] 
                    { 
                        HasLeftWrist, HasRightWrist, HasLeftElbow, 
                        HasRightElbow, HasLeftShoulder, HasRightShoulder 
                    };

                    if (!selectedLandmarks.Any(x => x))
                        return "At least one landmark must be selected";
                }

                return string.Empty;
            }
            catch
            {
                return "Invalid input values";
            }
        }

        private void Save()
        {
            if (_onSave == null) return;

            string error = ValidateInputs();
            if (!string.IsNullOrEmpty(error))
            {
                ValidationMessage = error;
                return;
            }

            var updatedElement = _element with
            {
                Action = new ActionConfig
                {
                    ClassName = SelectedClass,
                    MethodName = SelectedMethod.Id,
                    Arguments = ArgumentsWithDescriptions.Select(a => a.Value).ToList()
                }
            };

            if (IsPoseEnabled)
            {
                UpdateLandmarks();
                updatedElement = updatedElement with
                {
                    Sensitivity = Sensitivity,
                    Deadzone = Deadzone,
                    Linear = Linear,
                    File = "hit_trigger.py"
                };
            }

            _onSave(updatedElement);
            IsDialogOpen = false;
        }

        private void Cancel()
        {
            IsDialogOpen = false;
        }

        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
    }
}
