using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows.Input;
using System.IO;
using Newtonsoft.Json;
using MI_GUI_WinUI.Services;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionConfigurationDialogViewModel : ObservableObject
    {
    private readonly ActionService _actionService;
    private Action<UnifiedGuiElement>? _onSave;
    private UnifiedGuiElement _element;
    
    [ObservableProperty]
    private bool isDialogOpen;

    [ObservableProperty]
    private bool useCustomAction;

    [ObservableProperty]
    private ObservableCollection<ActionData> availableActions;

    [ObservableProperty]
    private ActionData? selectedCustomAction;

    private bool _isLoadingActions;
    public bool ShowBasicSettings => !UseCustomAction;
    public bool ShowCustomSettings => UseCustomAction;

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
        private string selectedLandmark = string.Empty;

        private readonly List<string> LandmarkOptions = new()
        {
            "LEFT_WRIST", "RIGHT_WRIST", 
            "LEFT_ELBOW", "RIGHT_ELBOW",
            "LEFT_SHOULDER", "RIGHT_SHOULDER"
        };

        private readonly string ACTIONS_DIR = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "MotionInput", "data", "assets", "generated_actions"
        );

        public ActionConfigurationDialogViewModel(ActionService actionService)
        {
            _actionService = actionService;
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

            // Load custom chain actions
            AvailableActions = new ObservableCollection<ActionData>();

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            LoadAvailableActions();

            SelectedClass = "ds4_gamepad";
            ArgumentsWithDescriptions = new ObservableCollection<ArgumentInfo>();
            SelectedMethod = AvailableMethods[0];
            UpdateArgumentInputs();
        }

        private async void LoadAvailableActions()
        {
            if (_isLoadingActions) return;
            _isLoadingActions = true;

            try
            {
                var actions = await _actionService.LoadActionsAsync();
                AvailableActions.Clear();
                foreach (var action in actions)
                {
                    AvailableActions.Add(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading actions: {ex.Message}");
            }
            finally
            {
                _isLoadingActions = false;
            }
        }

        public string HeaderText => IsPoseEnabled ? "Pose Detection Help" : 
            UseCustomAction ? "Custom Action Help" : "Available Buttons";

        public string HelpText => IsPoseEnabled ? 
            "• Sensitivity affects how quickly the pose is detected\n" +
            "• Deadzone sets minimum movement required\n" +
            "• Linear movement provides smoother transitions\n" +
            "• Select one landmark that should trigger the action" :
            "A, B, X, Y\n" +
            "LB, RB, LT, RT\n" +
            "Start, Back, LS, RS\n" +
            "DPad_Up, DPad_Down, DPad_Left, DPad_Right";

        public void Initialize(UnifiedGuiElement element, Action<UnifiedGuiElement> onSave)
        {
            _element = element;
            _onSave = onSave;

            UseCustomAction = element.Action.MethodName?.StartsWith("chain_") ?? false;
            
            SelectedClass = !string.IsNullOrEmpty(element.Action.ClassName) ? element.Action.ClassName : "ds4_gamepad";
            
            if (UseCustomAction)
            {
                var actionName = element.Action.MethodName?.Substring(6); // Remove "chain_" prefix
                if (!string.IsNullOrEmpty(actionName))
                {
                    SelectedCustomAction = AvailableActions.FirstOrDefault(a => a.Name == actionName);
                }
            }
            else if (!string.IsNullOrEmpty(element.Action.MethodName))
            {
                var method = AvailableMethods.FirstOrDefault(m => m.Id == element.Action.MethodName);
                if (method != null)
                {
                    SelectedMethod = method;
                    ArgumentsWithDescriptions.Clear();
                    if (element.Action.Arguments?.Any() == true)
                    {
                        if (method.Id.StartsWith("chain_"))
                        {
                            var actionName = method.Id.Substring(6);
                            var filePath = Path.Combine(ACTIONS_DIR, $"{actionName}.json");
                            
                            if (File.Exists(filePath))
                            {
                                var json = File.ReadAllText(filePath);
                                var actionData = JsonConvert.DeserializeObject<dynamic>(json);

                                ArgumentsWithDescriptions.Add(new ArgumentInfo(
                                    $"Custom action: {actionName}",
                                    JsonConvert.SerializeObject(actionData.action.args, Formatting.Indented),
                                    false
                                ));
                            }
                            else
                            {
                                ArgumentsWithDescriptions.Add(new ArgumentInfo(
                                    "Error: Custom action not found",
                                    "",
                                    false
                                ));
                            }
                        }
                        else
                        {
                            var descriptions = GetArgumentDescriptions(method.Id);
                            for (int i = 0; i < element.Action.Arguments.Count; i++)
                            {
                                string desc = i < descriptions.Length ? descriptions[i] : $"Argument {i + 1}";
                                string value = element.Action.Arguments[i]?.ToString() ?? "";
                                ArgumentsWithDescriptions.Add(new ArgumentInfo(desc, value, IsButtonArgument(method.Id, i)));
                            }
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

            OnPropertyChanged(nameof(ShowBasicSettings));
            OnPropertyChanged(nameof(ShowCustomSettings));
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(HelpText));

            // Set up pose settings
            IsPoseEnabled = element.IsPose;
            if (element.IsPose)
            {
                Sensitivity = element.Sensitivity ?? 1.0;
                Deadzone = element.Deadzone ?? 10;
                Linear = element.Linear ?? true;
                SelectedLandmark = element.Landmarks.FirstOrDefault() ?? "";
            }
            else
            {
                SelectedLandmark = "";
            }
            
            ValidationMessage = string.Empty;
            IsDialogOpen = true;
        }

        private bool IsButtonArgument(string methodId, int index)
        {
            if (UseCustomAction) return false;
            
            return methodId switch
            {
                "button_down" or "button_up" or "toggle_button" => true,
                "hold_button" or "press_button" => index == 0,
                _ => false
            };
        }

        private string[] GetArgumentDescriptions(string methodId)
        {
            if (methodId.StartsWith("chain_"))
            {
                return Array.Empty<string>();
            }

            return methodId switch
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
        }

        partial void OnSelectedMethodChanged(MethodDescription? value)
        {
            ValidationMessage = string.Empty;
            if (value != null) UpdateArgumentInputs();
        }

        partial void OnUseCustomActionChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowBasicSettings));
            OnPropertyChanged(nameof(ShowCustomSettings));
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(HelpText));
            ValidationMessage = string.Empty;
        }

        partial void OnIsPoseEnabledChanged(bool value)
        {
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(HelpText));
            
            if (value)
            {
                if (string.IsNullOrEmpty(SelectedLandmark))
                {
                    SelectedLandmark = LandmarkOptions[0];
                }
            }
            else
            {
                SelectedLandmark = "";
            }
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
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "A", true));
                    break;

                case "hold_button":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "A", true));
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[1], "0.5"));
                    break;

                case "press_button":
                    ArgumentsWithDescriptions.Add(new ArgumentInfo(descriptions[0], "A", true));
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
                if (UseCustomAction)
                {
                    if (SelectedCustomAction == null)
                        return "Please select an action";
                }
                else
                {
                    var arguments = ArgumentsWithDescriptions.Select(a => a.Value).ToList();
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
                }

                // Validate pose settings if enabled
                if (IsPoseEnabled)
                {
                    if (Sensitivity < 0.1 || Sensitivity > 2.0)
                        return "Sensitivity must be between 0.1 and 2.0";

                    if (Deadzone < 0 || Deadzone > 50)
                        return "Deadzone must be between 0 and 50";

                    if (string.IsNullOrEmpty(SelectedLandmark))
                        return "Please select a landmark";
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

            var updatedAction = new ActionConfig();

            if (UseCustomAction)
            {
                // Convert selected action into a chain action
                updatedAction = new ActionConfig
                {
                    ClassName = "ds4_gamepad",
                    MethodName = $"chain_{SelectedCustomAction!.Name}",
                    Arguments = SelectedCustomAction!.Sequence.Select(seq => seq.Type == "press" ?
                        new { type = "press", button = seq.Value } :
                        new { type = "sleep", duration = double.Parse(seq.Value) } as object
                    ).ToList()
                };
            }
            else
            {
                updatedAction = new ActionConfig
                {
                    ClassName = SelectedClass,
                    MethodName = SelectedMethod.Id,
                    Arguments = ArgumentsWithDescriptions.Select(a => 
                        float.TryParse(a.Value, out float number) ? number :
                        a.Value as object).ToList()
                };
            }

            var baseElement = _element.WithAction(updatedAction);

            var updatedElement = baseElement with
            {
                File = IsPoseEnabled ? "hit_trigger.py" : "button.py"
            };

            if (IsPoseEnabled)
            {
                updatedElement = updatedElement
                    .WithLandmarks(new List<string> { SelectedLandmark })
                    .WithPoseSettings(Sensitivity, Deadzone, Linear);
            }
            else
            {
                updatedElement = updatedElement
                    .WithLandmarks(new List<string>())
                    .WithPoseSettings(1.0, 10, true);
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
        public IEnumerable<string> LandmarkList => LandmarkOptions;
    }
}
