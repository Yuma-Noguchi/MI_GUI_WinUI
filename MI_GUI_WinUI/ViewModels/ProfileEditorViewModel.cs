using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Controls;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Utils;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly ILogger<ProfileEditorViewModel> _logger;
        private readonly string PROFILES_DIR = Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path, 
            "MotionInput", "data", "profiles"
        );

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        public bool HasValidationMessage
        {
            get => !string.IsNullOrWhiteSpace(ValidationMessage);
        }

        [ObservableProperty]
        private XamlRoot? xamlRoot;

        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();
        public ObservableCollection<UnifiedPositionInfo> CanvasElements { get; } = new();

        public ProfileEditorViewModel(ILogger<ProfileEditorViewModel> logger)
        {
            _logger = logger;
            LoadDefaultButtons();
            LoadCustomButtons();
        }

        private void LoadDefaultButtons()
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
                    IsDefault = true
                });
            }
        }

        private void LoadCustomButtons()
        {
            // TODO: Load custom buttons from IconStudio
        }

        [RelayCommand]
        public void NewProfile()
        {
            try
            {
                _logger?.LogInformation("Creating new profile");
                ProfileName = string.Empty;
                ValidationMessage = string.Empty;
                CanvasElements.Clear();
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
            CanvasElements.Clear();
            ValidationMessage = string.Empty;
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
                        await DialogHelper.ShowError("Please enter a name for the profile.", XamlRoot);
                    }
                    return;
                }

                if (!FileNameHelper.IsValidFileName(ProfileName))
                {
                    ValidationMessage = "The profile name contains invalid characters";
                    if (XamlRoot != null)
                    {
                        await DialogHelper.ShowError(
                            "The profile name contains invalid characters. Please use only letters, numbers, and basic punctuation.",
                            XamlRoot
                        );
                    }
                    return;
                }

                if (!Directory.Exists(PROFILES_DIR))
                {
                    Directory.CreateDirectory(PROFILES_DIR);
                }

                var sanitizedName = FileNameHelper.SanitizeFileName(ProfileName);
                var filePath = Path.Combine(PROFILES_DIR, $"{sanitizedName}.json");

                // Check if file exists
                if (File.Exists(filePath) && XamlRoot != null)
                {
                    var overwrite = await DialogHelper.ShowConfirmation(
                        $"A profile named '{sanitizedName}.json' already exists.\nDo you want to replace it?",
                        "Replace Existing Profile?",
                        XamlRoot
                    );

                    if (!overwrite)
                    {
                        return;
                    }
                }

                // Convert UnifiedGuiElements to appropriate types
                var guiElements = new List<GuiElement>();
                var poseElements = new List<PoseGuiElement>();

                foreach (var elementInfo in CanvasElements)
                {
                    if (elementInfo.Element.IsPose)
                    {
                        poseElements.Add(elementInfo.Element.ToPoseElement());
                    }
                    else
                    {
                        guiElements.Add(elementInfo.Element.ToGuiElement());
                    }
                }

                var profile = new Profile
                {
                    Name = sanitizedName,
                    GlobalConfig = new Dictionary<string, string>() { { "grid_snap", "false" } },
                    GuiElements = guiElements,
                    Poses = poseElements,
                    SpeechCommands = new Dictionary<string, SpeechCommand>()
                };

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                ValidationMessage = "Profile saved successfully";
                
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowMessage(
                        $"Profile saved as {sanitizedName}.json",
                        "Success",
                        XamlRoot
                    );
                }

                _logger?.LogInformation($"Successfully saved profile: {filePath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving profile");
                ValidationMessage = $"Error saving profile: {ex.Message}";
                
                if (XamlRoot != null)
                {
                    await DialogHelper.ShowError(
                        "Failed to save profile. Please try again.",
                        XamlRoot
                    );
                }
            }
        }

        public async Task LoadProfile(Profile profile)
        {
            try
            {
                _logger?.LogInformation($"Loading profile {profile.Name}");
                ProfileName = profile.Name;
                CanvasElements.Clear();

                // Convert GuiElements to UnifiedGuiElements
                if (profile.GuiElements != null)
                {
                    foreach (var element in profile.GuiElements)
                    {
                        var unifiedElement = UnifiedGuiElement.FromGuiElement(element);
                        await AddElement(unifiedElement, ElementType.Button);
                    }
                }

                // Convert PoseElements to UnifiedGuiElements
                if (profile.Poses != null)
                {
                    foreach (var element in profile.Poses)
                    {
                        var unifiedElement = UnifiedGuiElement.FromPoseElement(element);
                        await AddElement(unifiedElement, ElementType.Pose);
                    }
                }

                ValidationMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error loading profile {profile.Name}");
                ValidationMessage = $"Error loading profile: {ex.Message}";
                throw;
            }
        }

        private async Task AddElement(UnifiedGuiElement element, ElementType type)
        {
            if (element.Position.Count != 2) return;

            double width = element.Radius * 2;
            double height = element.Radius * 2;
            Point position = new Point(element.Position[0] - width/2, element.Position[1] - height/2);

            var request = new ElementAddRequest(element, position, type);
            AddElementToCanvas(request);
        }

        public void AddElementToCanvas(ElementAddRequest request)
        {
            var elementInfo = new UnifiedPositionInfo(
                request.Element,
                request.Position,
                new Size(request.Element.Radius * 2, request.Element.Radius * 2)
            );

            CanvasElements.Add(elementInfo);
            _logger?.LogInformation($"Added element to canvas: {request.Element.File} at position {request.Position.X}, {request.Position.Y}");
        }

        public void UpdateElementPosition(UnifiedPositionInfo info)
        {
            var index = CanvasElements.IndexOf(info);
            if (index >= 0)
            {
                // Update element with new center position
                var updatedElement = info.Element.WithPosition(
                    (int)(info.Position.X + info.Size.Width/2),
                    (int)(info.Position.Y + info.Size.Height/2)
                );

                CanvasElements[index] = new UnifiedPositionInfo(
                    updatedElement,
                    info.Position,
                    info.Size
                );
            }
        }

        public async Task ConfigureAction(UnifiedPositionInfo elementInfo)
        {
            if (XamlRoot == null) return;

            var dialog = new ActionConfigurationDialog();
            dialog.XamlRoot = XamlRoot;
            
            dialog.Configure(elementInfo.Element, updatedElement =>
            {
                var index = CanvasElements.IndexOf(elementInfo);
                if (index >= 0)
                {
                    CanvasElements[index] = new UnifiedPositionInfo(
                        updatedElement,
                        elementInfo.Position,
                        elementInfo.Size
                    );
                }
            });

            await dialog.ShowAsync();
        }
    }
}
