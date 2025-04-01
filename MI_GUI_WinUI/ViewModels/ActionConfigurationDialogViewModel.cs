using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionConfigurationDialogViewModel : ViewModelBase
    {
        private readonly IActionService _actionService;
        private UnifiedGuiElement _element = new();
        private Action<UnifiedGuiElement>? _onSave;

        [ObservableProperty]
        private bool isDialogOpen;

        [ObservableProperty]
        private bool useCustomAction;

        [ObservableProperty]
        private ObservableCollection<ActionData> availableActions = new();

        [ObservableProperty]
        private ActionData? selectedCustomAction;

        public bool ShowBasicSettings => !UseCustomAction;
        public bool ShowCustomSettings => UseCustomAction;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        [ObservableProperty]
        private ObservableCollection<MethodDescription> availableMethods = new();

        [ObservableProperty]
        private string selectedClass = "ds4_gamepad";

        [ObservableProperty]
        private MethodDescription selectedMethod = null!;

        [ObservableProperty]
        private ObservableCollection<ArgumentInfo> argumentsWithDescriptions = new();

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

        [ObservableProperty]
        private string headerText = "Available Buttons";

        [ObservableProperty]
        private string helpText = "A, B, X, Y\nLB, RB, LT, RT\nStart, Back, LS, RS\nDPad_Up, DPad_Down, DPad_Left, DPad_Right";

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
        
        public IEnumerable<string> LandmarkList => LandmarkOptions;

        public ActionConfigurationDialogViewModel(
            IActionService actionService,
            ILogger<ActionConfigurationDialogViewModel> logger,
            INavigationService navigationService)
            : base(logger, navigationService)
        {
            _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
            
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

            SelectedMethod = AvailableMethods[0];
            _ = LoadAvailableActionsAsync();
        }

        public async Task Configure(UnifiedGuiElement element, Action<UnifiedGuiElement> onSave)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _element = element ?? throw new ArgumentNullException(nameof(element));
                _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));

                UseCustomAction = element.Action.MethodName == "chain" || (element.Action.MethodName?.StartsWith("chain_") ?? false);
                SelectedClass = !string.IsNullOrEmpty(element.Action.ClassName) ? element.Action.ClassName : "ds4_gamepad";

                if (UseCustomAction)
                {
                    await SetupCustomAction(element);
                }
                else if (!string.IsNullOrEmpty(element.Action.MethodName))
                {
                    await SetupStandardAction(element);
                }
                else
                {
                    SelectedMethod = AvailableMethods.First();
                    UpdateArgumentInputs();
                }

                SetupPoseSettings(element);

                ValidationMessage = string.Empty;
                IsDialogOpen = true;

                await Task.CompletedTask;
            }, nameof(Configure));
        }

        private async Task LoadAvailableActionsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var actions = await _actionService.LoadActionsAsync();
                AvailableActions.Clear();
                foreach (var action in actions)
                {
                    AvailableActions.Add(action);
                }
            }, nameof(LoadAvailableActionsAsync));
        }

        private async Task SetupCustomAction(UnifiedGuiElement element)
        {
            string? actionName = null;
            if (element.Action.MethodName == "chain")
            {
                if (element.Action.Arguments?.Count > 0 && element.Action.Arguments[0] is Dictionary<string, object> firstArg)
                {
                    if (firstArg.TryGetValue("name", out var nameObj))
                    {
                        actionName = nameObj?.ToString();
                    }
                }
            }
            else if (element.Action.MethodName?.StartsWith("chain_") ?? false)
            {
                actionName = element.Action.MethodName.Substring(6);
            }

            if (!string.IsNullOrEmpty(actionName))
            {
                if (AvailableActions.Count == 0)
                {
                    await LoadAvailableActionsAsync();
                }
                SelectedCustomAction = AvailableActions.FirstOrDefault(a => a.Name == actionName);
            }
        }

        private Task SetupStandardAction(UnifiedGuiElement element)
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
                        string value = element.Action.Arguments[i]?.ToString() ?? "";
                        ArgumentsWithDescriptions.Add(new ArgumentInfo(desc, value, IsButtonArgument(method.Id, i)));
                    }
                }
                else
                {
                    UpdateArgumentInputs();
                }
            }

            return Task.CompletedTask;
        }

        private void SetupPoseSettings(UnifiedGuiElement element)
        {
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

        [RelayCommand]
        private async Task SaveAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
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
                    updatedAction = new ActionConfig
                    {
                        ClassName = "ds4_gamepad",
                        MethodName = "chain",
                        Arguments = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                { "name", SelectedCustomAction!.Name },
                                { "sequence", SelectedCustomAction!.Sequence.Select(seq => seq.Type == "press" ?
                                    new { type = "press", button = seq.Value } :
                                    new { type = "sleep", duration = double.Parse(seq.Value) } as object
                                ).ToList() }
                            }
                        }
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
                var updatedElement = baseElement with { File = IsPoseEnabled ? "hit_trigger.py" : "button.py" };

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
            }, nameof(SaveAsync));
        }

        [RelayCommand]
        private void Cancel()
        {
            IsDialogOpen = false;
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

        partial void OnSelectedMethodChanged(MethodDescription? value)
        {
            ValidationMessage = string.Empty;
            if (value != null) UpdateArgumentInputs();
        }

        partial void OnUseCustomActionChanged(bool value)
        {
            if (value)
            {
                HeaderText = "Custom Action Help";
            }
            else
            {
                HeaderText = "Available Buttons";
            }
            OnPropertyChanged(nameof(ShowBasicSettings));
            OnPropertyChanged(nameof(ShowCustomSettings));
            ValidationMessage = string.Empty;
        }

        partial void OnIsPoseEnabledChanged(bool value)
        {
            if (value)
            {
                HeaderText = "Pose Detection Help";
                HelpText = "• Sensitivity affects how quickly the pose is detected\n" +
                          "• Deadzone sets minimum movement required\n" +
                          "• Linear movement provides smoother transitions\n" +
                          "• Select one landmark that should trigger the action";

                if (string.IsNullOrEmpty(SelectedLandmark))
                {
                    SelectedLandmark = LandmarkOptions[0];
                }
            }
            else
            {
                HeaderText = "Available Buttons";
                HelpText = "A, B, X, Y\n" +
                          "LB, RB, LT, RT\n" +
                          "Start, Back, LS, RS\n" +
                          "DPad_Up, DPad_Down, DPad_Left, DPad_Right";
                SelectedLandmark = "";
            }
        }

        protected override async Task ShowErrorAsync(string message)
        {
            ValidationMessage = message;
            await Task.CompletedTask;
        }

        public override void Cleanup()
        {
            try
            {
                ArgumentsWithDescriptions?.Clear();
                AvailableActions?.Clear();
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
