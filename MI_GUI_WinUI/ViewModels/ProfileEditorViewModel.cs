using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Windows.Foundation;
using MI_GUI_WinUI.Models;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly ILogger<ProfileEditorViewModel> _logger;
        private readonly ProfileService _profileService;
        private readonly string _baseAppPath;
        
        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();
        public ObservableCollection<EditorButton> CanvasButtons { get; } = new();

        [ObservableProperty]
        private string _profileName = string.Empty;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private bool _isGridSnapEnabled;

        private const int GRID_SIZE = 20; // Grid size in pixels

        public ProfileEditorViewModel(ILogger<ProfileEditorViewModel> logger, ProfileService profileService)
        {
            _logger = logger;
            _profileService = profileService;
            _baseAppPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;

            InitializeDefaultButtons();
            InitializeCustomButtons();
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
                    File = $"{button.BasePath}.png", // Store just the filename in File property
                    Category = "Default",
                    IconPath = normalImagePath,
                    Skin = $"{button.BasePath}.png",
                    TriggeredSkin = $"{button.BasePath}_triggered.png",
                    Radius = 30 // Default size for all buttons
                });
            }
        }

        private void InitializeCustomButtons()
        {
            // Custom buttons will be loaded from user's saved buttons
            // TODO: Implement custom button loading from IconStudio
        }

        [RelayCommand]
        private void AddButtonToCanvas(ButtonPositionInfo info)
        {
            try
            {
                var newButton = info.Button.Clone();
                newButton.Position = GetSnappedPosition(info.Position);

                CanvasButtons.Add(newButton);
                ValidationMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding button to canvas");
                ValidationMessage = "Failed to add button to canvas";
            }
        }

        [RelayCommand]
        private void UpdateButtonPosition(ButtonPositionInfo info)
        {
            try
            {
                if (info.Button != null)
                {
                    var position = GetSnappedPosition(info.Position);
                    info.Button.Position = position;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating button position");
                ValidationMessage = "Failed to update button position";
            }
        }

        [RelayCommand]
        private void RemoveButton(EditorButton button)
        {
            if (button != null)
            {
                CanvasButtons.Remove(button);
                ValidationMessage = string.Empty;
            }
        }

        private Point GetSnappedPosition(Point position)
        {
            if (!IsGridSnapEnabled)
                return position;

            return new Point(
                Math.Round(position.X / GRID_SIZE) * GRID_SIZE,
                Math.Round(position.Y / GRID_SIZE) * GRID_SIZE
            );
        }

        [RelayCommand]
        private async Task SaveProfile()
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
                    GlobalConfig = new System.Collections.Generic.Dictionary<string, string>(),
                    GuiElements = CanvasButtons.Select(b => b.ToGuiElement()).ToList(),
                    Poses = new System.Collections.Generic.List<PoseConfig>(),
                    SpeechCommands = new System.Collections.Generic.Dictionary<string, SpeechCommand>()
                };

                await _profileService.SaveProfilesToJsonAsync(
                    new System.Collections.Generic.List<Profile> { profile }, 
                    Path.Combine("MotionInput", "data", "profiles")
                );

                ValidationMessage = "Profile saved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                ValidationMessage = "Failed to save profile";
            }
        }

        [RelayCommand]
        private async Task LoadProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    ValidationMessage = "Please enter a profile name";
                    return;
                }

                var profile = _profileService.GetProfileFromCache(ProfileName);
                if (profile == null)
                {
                    ValidationMessage = "Profile not found";
                    return;
                }

                // Clear existing buttons
                CanvasButtons.Clear();

                // Add buttons from the loaded profile
                foreach (var element in profile.Value.GuiElements)
                {
                    CanvasButtons.Add(EditorButton.FromGuiElement(element));
                }

                ValidationMessage = "Profile loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                ValidationMessage = "Failed to load profile";
            }
        }

        [RelayCommand]
        private void NewProfile()
        {
            ProfileName = string.Empty;
            CanvasButtons.Clear();
            ValidationMessage = string.Empty;
        }
    }
}
