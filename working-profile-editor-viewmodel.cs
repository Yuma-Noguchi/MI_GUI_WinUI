using System;
using System.Linq;
using System.Collections.ObjectModel;
using Windows.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly ILogger<ProfileEditorViewModel> _logger;
        private readonly string _baseAppPath;
        private readonly string PROFILES_DIR = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput", "data", "profiles");
        private XamlRoot? _xamlRoot;

        public ProfileEditorViewModel(ILogger<ProfileEditorViewModel> logger)
        {
            _logger = logger;
            _baseAppPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            InitializeDefaultButtons();
            LoadCustomButtons();
            InitializeDefaultPoses();
        }

        private void InitializeDefaultPoses()
        {
            var defaultPoses = new[]
            {
                new PoseGuiElement 
                {
                    File = "head_tilt_joystick.py",
                    LeftSkin = ConvertToMsAppxPath("racing/left_arrow.png"),
                    RightSkin = ConvertToMsAppxPath("racing/right_arrow.png"),
                    Sensitivity = 0.75,
                    Deadzone = 1,
                    Linear = false
                },
                new PoseGuiElement
                {
                    File = "hit_trigger.py",
                    Landmark = "right_wrist",
                    Skin = ConvertToMsAppxPath("racing/forward.png"),
                    Radius = 60,
                    Action = new ActionConfig
                    {
                        ClassName = "ds4_gamepad",
                        MethodName = "right_trigger",
                        Arguments = new List<string> { "0.75" }
                    }
                },
                new PoseGuiElement
                {
                    File = "hit_trigger.py",
                    Landmark = "left_wrist",
                    Skin = ConvertToMsAppxPath("racing/backward.png"),
                    Radius = 60,
                    Action = new ActionConfig
                    {
                        ClassName = "ds4_gamepad",
                        MethodName = "left_trigger",
                        Arguments = new List<string> { "1.0" }
                    }
                }
            };

            foreach (var pose in defaultPoses)
            {
                DefaultPoses.Add(pose);
            }
        }

        public XamlRoot? XamlRoot
        {
            get => _xamlRoot;
            set => _xamlRoot = value;
        }

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();
        public ObservableCollection<ButtonPositionInfo> CanvasButtons { get; } = new();
        public ObservableCollection<PoseGuiElement> DefaultPoses { get; } = new();
        public ObservableCollection<PosePositionInfo> CanvasPoses { get; } = new();

        private void PrepareForEdit()
        {
            CanvasButtons.Clear();
            CanvasPoses.Clear();
            ValidationMessage = string.Empty;
        }

        public async Task LoadExistingProfile(Profile profile)
        {
            try
            {
                PrepareForEdit();
                
                // Set profile name
                ProfileName = profile.Name;

                // Load GUI elements
                if (profile.GuiElements != null)
                {
                    _logger?.LogInformation($"Loading {profile.GuiElements.Count} GUI elements for profile {profile.Name}");
                    
                    foreach (var element in profile.GuiElements)
                    {
                        try
                        {
                            var buttonInfo = ConvertFromGuiElement(element);
                            if (buttonInfo != null)
                            {
                                CanvasButtons.Add(buttonInfo);
                                _logger?.LogInformation($"Added GUI element: {element.File} at position {element.Position[0]}, {element.Position[1]}");
                            }
                        }
                        catch (Exception elementEx)
                        {
                            _logger?.LogError(elementEx, $"Error loading GUI element: {element.File}");
                        }
                    }
                }

                // Load poses
                if (profile.Poses != null)
                {
                    _logger?.LogInformation($"Loading {profile.Poses.Count} poses for profile {profile.Name}");

                    foreach (var pose in profile.Poses)
                    {
                        try
                        {
                            var poseInfo = ConvertFromPoseElement(pose);
                            CanvasPoses.Add(poseInfo);
                            _logger?.LogInformation($"Added pose: {pose.File} at position {pose.Position?[0]}, {pose.Position?[1]}");
                        }
                        catch (Exception poseEx)
                        {
                            _logger?.LogError(poseEx, $"Error loading pose: {pose.File}");
                        }
                    }
                }

                _logger?.LogInformation($"Successfully loaded profile {profile.Name} with {CanvasButtons.Count} GUI elements and {CanvasPoses.Count} poses");
                ValidationMessage = "Profile loaded successfully";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error loading profile: {profile.Name}");
                ValidationMessage = $"Error loading profile: {ex.Message}";
                throw;
            }
        }

        private void InitializeDefaultButtons()
        {
            var defaultButtons = new[]
            {
                new { Name = "A Button", BasePath = "a" },
                new { Name = "B Button", BasePath = "b" },
                new { Name = "X Button", BasePath = "x" },
                new { Name = "Y Button", BasePath = "y" },
                new { Name = "D-Pad Up", BasePath = "up" },
                new { Name = "D-Pad Down", BasePath = "down" },
                new { Name = "D-Pad Left", BasePath = "left" },
                new { Name = "D-Pad Right", BasePath = "right" },
                new { Name = "Gamepad", BasePath = "gamepad" }
            };

            foreach (var button in defaultButtons)
            {
                // Create relative paths for WinUI resource loading
                string basePath = Path.Combine("MotionInput", "data", "assets", "gamepad");
                string baseResourcePath = $"ms-appx:///{basePath}";
                string normalImagePath = $"{baseResourcePath}/{button.BasePath}.png";

                DefaultButtons.Add(new EditorButton
                {
                    Name = button.Name,
                    IconPath = normalImagePath,
                    Category = "Default",
                    Action = null,
                    IsDefault = true
                });
            }
        }

        private void LoadCustomButtons()
        {
            // TODO: Load custom buttons from IconStudio
        }

        [RelayCommand]
        public void AddButtonToCanvas(ButtonPositionInfo buttonInfo)
        {
            CanvasButtons.Add(buttonInfo);
        }

        public void UpdateButtonPosition(ButtonPositionInfo buttonInfo)
        {
            var existingButton = CanvasButtons.FirstOrDefault(b => b.Button.Name == buttonInfo.Button.Name);
            if (existingButton != null)
            {
                var index = CanvasButtons.IndexOf(existingButton);
                CanvasButtons[index] = new ButtonPositionInfo
                {
                    Button = buttonInfo.Button,
                    Position = buttonInfo.Position,
                    Size = buttonInfo.Size
                };
            }
        }

        [RelayCommand]
        public void NewProfile()
        {
            try
            {
                _logger?.LogInformation("Creating new profile");
                ProfileName = string.Empty;
                ValidationMessage = string.Empty;
                CanvasButtons.Clear();
                CanvasPoses.Clear();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating new profile");
                ValidationMessage = "Error creating new profile";
            }
        }

        [RelayCommand]
        public void ClearCanvas()
        {
            CanvasButtons.Clear();
            CanvasPoses.Clear();
            ValidationMessage = string.Empty;
        }

        private ActionConfig CreateDefaultActionConfig(EditorButton button)
        {
            return new ActionConfig
            {
                ClassName = "XboxController",
                MethodName = "PressButton",
                Arguments = new List<string> { button.Name }
            };
        }

        private GuiElement ConvertToGuiElement(ButtonPositionInfo buttonInfo)
        {
            // Calculate center position by adding radius to top-left corner
            int radius = (int)(buttonInfo.Size.Width / 2);
            return new GuiElement
            {
                File = buttonInfo.Button.Name,
                Position = new List<int> { 
                    (int)(buttonInfo.Position.X + radius),  // X center
                    (int)(buttonInfo.Position.Y + radius)   // Y center
                },
                Radius = radius,
                Skin = buttonInfo.Button.IconPath,
                Action = CreateDefaultActionConfig(buttonInfo.Button)
            };
        }

        private ButtonPositionInfo ConvertFromGuiElement(GuiElement element)
        {
            // Try to find existing button first
            var sourceButton = FindSourceButton(element.File);
            
            // If found, use its paths, otherwise construct paths from the JSON data
            var button = sourceButton?.Clone() ?? new EditorButton
            {
                Name = element.File,
                IconPath = ConvertToMsAppxPath(element.Skin)
            };

            // Calculate top-left position by subtracting radius from center
            var diameter = element.Radius * 2;
            return new ButtonPositionInfo
            {
                Button = button,
                Position = new Point(
                    element.Position[0] - element.Radius,  // X top-left
                    element.Position[1] - element.Radius   // Y top-left
                ),
                Size = new Size(diameter, diameter)
            };
        }

        private string ConvertToMsAppxPath(string relativePath)
        {
            if (relativePath.StartsWith("ms-appx:///"))
                return relativePath;

            // Remove any leading slashes and combine with base path
            relativePath = relativePath.TrimStart('/');
            return $"ms-appx:///MotionInput/data/assets/{relativePath}";
        }

        [RelayCommand]
        public async Task SaveProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    ValidationMessage = "Please enter a profile name";
                    if (XamlRoot != null)
                    {
                        await Utils.DialogHelper.ShowError("Please enter a name for the profile.", XamlRoot);
                    }
                    return;
                }

                if (!Utils.FileNameHelper.IsValidFileName(ProfileName))
                {
                    ValidationMessage = "The profile name contains invalid characters";
                    if (XamlRoot != null)
                    {
                        await Utils.DialogHelper.ShowError("The profile name contains invalid characters. Please use only letters, numbers, and basic punctuation.", XamlRoot);
                    }
                    return;
                }

                if (!Directory.Exists(PROFILES_DIR))
                {
                    Directory.CreateDirectory(PROFILES_DIR);
                }

                var sanitizedName = Utils.FileNameHelper.SanitizeFileName(ProfileName);
                var filePath = Path.Combine(PROFILES_DIR, $"{sanitizedName}.json");

                // Check if file exists
                if (File.Exists(filePath) && XamlRoot != null)
                {
                    var overwrite = await Utils.DialogHelper.ShowConfirmation(
                        $"A profile named '{sanitizedName}.json' already exists.\nDo you want to replace it?",
                        "Replace Existing Profile?",
                        XamlRoot);

                    if (!overwrite)
                    {
                        return;
                    }
                }

                var profile = new Profile
                {
                    Name = sanitizedName,
                    GlobalConfig = new Dictionary<string, string>(),
                    GuiElements = CanvasButtons.Select(ConvertToGuiElement).ToList(),
                    Poses = CanvasPoses.Select(p => p.Pose).ToList(),
                    SpeechCommands = new Dictionary<string, SpeechCommand>()
                };

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                ValidationMessage = "Profile saved successfully";
                
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowMessage($"Profile saved as {sanitizedName}.json", "Success", XamlRoot);
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error saving profile: {ex.Message}";
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("Failed to save profile. Please try again.", XamlRoot);
                }
            }
        }

        [RelayCommand]
        public async Task LoadProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    ValidationMessage = "Please enter a profile name to load";
                    return;
                }

                var filePath = Path.Combine(PROFILES_DIR, $"{ProfileName}.json");
                if (!File.Exists(filePath))
                {
                    ValidationMessage = "Profile not found";
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var profile = JsonConvert.DeserializeObject<Profile>(json);

                if (profile.GuiElements == null)
                {
                    ValidationMessage = "Error loading profile: Invalid format";
                    return;
                }

                CanvasButtons.Clear();
                foreach (var element in profile.GuiElements)
                {
                    CanvasButtons.Add(ConvertFromGuiElement(element));
                }

                foreach (var pose in profile.Poses)
                {
                    var poseInfo = ConvertFromPoseElement(pose);
                    CanvasPoses.Add(poseInfo);
                }

                ValidationMessage = "Profile loaded successfully";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error loading profile: {ex.Message}";
            }
        }

        private EditorButton? FindSourceButton(string buttonName)
        {
            return DefaultButtons.FirstOrDefault(b => b.Name == buttonName) ??
                   CustomButtons.FirstOrDefault(b => b.Name == buttonName);
        }

        [RelayCommand]
        public void AddPoseToCanvas(PosePositionInfo poseInfo)
        {
            CanvasPoses.Add(poseInfo);
        }

        public void UpdatePosePosition(PosePositionInfo poseInfo)
        {
            var existingPose = CanvasPoses.FirstOrDefault(p => p.Pose.File == poseInfo.Pose.File);
            if (existingPose != null)
            {
                var index = CanvasPoses.IndexOf(existingPose);
                CanvasPoses[index] = poseInfo.Clone();
            }
        }

        private PosePositionInfo ConvertFromPoseElement(PoseGuiElement pose)
        {
            double radius = pose.Radius;
            double x = pose.Position?[0] ?? 0;
            double y = pose.Position?[1] ?? 0;

            return new PosePositionInfo
            {
                Pose = pose,
                Position = new Point(x - radius, y - radius),
                Size = new Size(radius * 2, radius * 2)
            };
        }
    }
}
