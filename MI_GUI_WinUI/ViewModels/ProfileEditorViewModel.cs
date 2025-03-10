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

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly string _baseAppPath;
        private readonly string PROFILES_DIR = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput", "data", "profiles");

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();
        public ObservableCollection<ButtonPositionInfo> CanvasButtons { get; } = new();

        public ProfileEditorViewModel()
        {
            _baseAppPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            InitializeDefaultButtons();
            LoadCustomButtons();
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
                string triggeredImagePath = $"{baseResourcePath}/{button.BasePath}_triggered.png";

                DefaultButtons.Add(new EditorButton
                {
                    Name = button.Name,
                    IconPath = normalImagePath, // Store just the filename in File property
                    TriggeredIconPath = triggeredImagePath,
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
            ProfileName = string.Empty;
            ValidationMessage = string.Empty;
            CanvasButtons.Clear();
        }

        [RelayCommand]
        public void ClearCanvas()
        {
            CanvasButtons.Clear();
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
            return new GuiElement
            {
                File = buttonInfo.Button.Name,
                Position = new List<int> { (int)buttonInfo.Position.X, (int)buttonInfo.Position.Y },
                Radius = (int)(buttonInfo.Size.Width / 2),
                Skin = buttonInfo.Button.IconPath,
                TriggeredSkin = buttonInfo.Button.GetTriggeredIconPath(),
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
                IconPath = ConvertToMsAppxPath(element.Skin),
                TriggeredIconPath = ConvertToMsAppxPath(element.TriggeredSkin)
            };

            return new ButtonPositionInfo
            {
                Button = button,
                Position = new Point(element.Position[0], element.Position[1]),
                Size = new Size(element.Radius * 2, element.Radius * 2)
            };
        }

        private string ConvertToMsAppxPath(string relativePath)
        {
            if (relativePath.StartsWith("ms-appx:///"))
                return relativePath;

            // Remove any leading slashes and combine with base path
            relativePath = relativePath.TrimStart('/');
            return $"ms-appx:///{relativePath}";
        }

        [RelayCommand]
        public async Task SaveProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    ValidationMessage = "Please enter a profile name";
                    return;
                }

                var profile = new Profile
                {
                    Name = ProfileName,
                    GlobalConfig = new Dictionary<string, string>(),
                    GuiElements = CanvasButtons.Select(ConvertToGuiElement).ToList(),
                    Poses = new List<PoseConfig>(),
                    SpeechCommands = new Dictionary<string, SpeechCommand>()
                };

                if (!Directory.Exists(PROFILES_DIR))
                {
                    Directory.CreateDirectory(PROFILES_DIR);
                }

                var filePath = Path.Combine(PROFILES_DIR, $"{ProfileName}.json");
                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                ValidationMessage = "Profile saved successfully";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error saving profile: {ex.Message}";
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

    }
}
