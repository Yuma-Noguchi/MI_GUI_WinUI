using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly ProfileService _profileService;
        private const float DROPPED_IMAGE_SIZE = 80;
        private readonly string PROFILES_DIR = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput", "data", "profiles");

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private XamlRoot? xamlRoot;

        [ObservableProperty]
        private bool shouldClearCanvas;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        partial void OnValidationMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasValidationMessage));
        }

        public ObservableCollection<UnifiedPositionInfo> CanvasElements { get; } = new();
        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();

        public ProfileEditorViewModel(ProfileService profileService)
        {
            _profileService = profileService;
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
                new { Name = "Hit Trigger", BasePath = "hit_trigger" }
            };

            foreach (var button in defaultButtons)
            {
                string relativePath = $"gamepad/{button.BasePath}.png";
                string displayPath = Utils.FileNameHelper.GetFullAssetPath(relativePath);

                DefaultButtons.Add(new EditorButton
                {
                    Name = button.Name,
                    IconPath = displayPath,
                    FileName = relativePath,
                    IsDefault = true
                });
            }
        }

        private void LoadCustomButtons()
        {
            var iconsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, 
                                       "MotionInput", "data", "assets", "generated_icons");

            if (Directory.Exists(iconsPath))
            {
                try
                {
                    var files = Directory.GetFiles(iconsPath);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        string relativePath = $"generated_icons/{Path.GetFileName(file)}";
                        string displayPath = Utils.FileNameHelper.GetFullAssetPath(relativePath);
                        
                        CustomButtons.Add(new EditorButton(
                            name: fileName,
                            iconPath: displayPath,
                            fileName: relativePath,
                            isDefault: false
                        ));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading custom buttons: {ex.Message}");
                }
            }
        }

        private void PrepareForEdit()
        {
            CanvasElements.Clear();
            ValidationMessage = string.Empty;
        }

        public async Task LoadExistingProfile(Profile profile)
        {
            try
            {
                ShouldClearCanvas = true;
                PrepareForEdit();
                ProfileName = profile.Name;

                // Load GUI elements
                if (profile.GuiElements != null)
                {
                    foreach (var element in profile.GuiElements)
                    {
                        var unifiedElement = UnifiedGuiElement.FromGuiElement(element);
                        AddElementToCanvas(ElementAddRequest.FromExisting(
                            unifiedElement, 
                            new Point(element.Position[0], element.Position[1])
                        ));
                    }
                }

                // Load poses
                if (profile.Poses != null)
                {
                    foreach (var pose in profile.Poses)
                    {
                        // if skin is set, add element with skin
                        if (!string.IsNullOrEmpty(pose.Skin))
                        {
                            var unifiedElement = UnifiedGuiElement.FromPoseElement(pose);
                            AddElementToCanvas(ElementAddRequest.FromExisting(
                            unifiedElement,
                            new Point(pose.Position[0], pose.Position[1])
                            ));
                        }   
                    }
                }

                ValidationMessage = "Profile loaded successfully";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error loading profile: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddButtonToCanvas(UnifiedPositionInfo buttonInfo)
        {
            CanvasElements.Add(buttonInfo);
        }

        public void AddElementToCanvas(ElementAddRequest request)
        {
            var element = request.Element;
            var updatedElement = element with 
            {
                // Update skin to be relative path if FileName is set in EditorButton
                Skin = request.Button?.FileName ?? element.Skin
            };

            var info = new UnifiedPositionInfo(
                updatedElement,
                request.Position,
                new Size(DROPPED_IMAGE_SIZE, DROPPED_IMAGE_SIZE)
            );

            CanvasElements.Add(info);
        }

        public void UpdateElementPosition(UnifiedPositionInfo info, int index)
        {
            if (index >= 0 && index < CanvasElements.Count)
            {
                CanvasElements[index] = info;
            }
        }

        [RelayCommand]
        public void NewProfile()
        {
            ShouldClearCanvas = true;
            ProfileName = string.Empty;
            ValidationMessage = string.Empty;
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
                        await Utils.DialogHelper.ShowError("Please enter a name for the profile.", XamlRoot);
                    }
                    return;
                }

                if (!Utils.FileNameHelper.IsValidFileName(ProfileName))
                {
                    ValidationMessage = "The profile name contains invalid characters";
                    if (XamlRoot != null)
                    {
                        await Utils.DialogHelper.ShowError("The profile name contains invalid characters.", XamlRoot);
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
                        $"A profile named '{sanitizedName}.json' already exists. Do you want to replace it?",
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
                    GuiElements = new List<GuiElement>(),
                    Poses = new List<PoseGuiElement>(),
                    GlobalConfig = new Dictionary<string, string>(),
                    SpeechCommands = new Dictionary<string, SpeechCommand>()
                };

                // Split elements into GUI and Pose arrays
                foreach (var element in CanvasElements)
                {
                    if (element.Element.IsPose)
                    {
                        profile.Poses.Add(element.Element.ToPoseElement());
                    }
                    else
                    {
                        profile.GuiElements.Add(element.Element.ToGuiElement());
                    }
                }

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                // Clear profile cache to ensure fresh data on next load
                _profileService.ClearCache();

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
                ShouldClearCanvas = true;
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

                PrepareForEdit();

                // Load GUI elements
                if (profile.GuiElements != null)
                {
                    foreach (var element in profile.GuiElements)
                    {
                        var unifiedElement = UnifiedGuiElement.FromGuiElement(element);
                        AddElementToCanvas(ElementAddRequest.FromExisting(
                            unifiedElement, 
                            new Point(element.Position[0], element.Position[1])
                        ));
                    }
                }

                // Load poses
                if (profile.Poses != null)
                {
                    foreach (var pose in profile.Poses)
                    {
                        var unifiedElement = UnifiedGuiElement.FromPoseElement(pose);
                        AddElementToCanvas(ElementAddRequest.FromExisting(
                            unifiedElement,
                            new Point(pose.Position[0], pose.Position[1])
                        ));
                    }
                }

                ValidationMessage = "Profile loaded successfully";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error loading profile: {ex.Message}";
            }
        }

        public async Task ConfigureAction(UnifiedPositionInfo elementInfo, ResizableImage image)
        {
            if (XamlRoot == null) return;

            var dialog = new ActionConfigurationDialog();
            dialog.XamlRoot = XamlRoot;
            var index = CanvasElements.IndexOf(elementInfo);

            dialog.Configure(elementInfo.Element, element => 
            {
                // Create updated element with the same position and size
                var updatedInfo = new UnifiedPositionInfo(
                    element,
                    elementInfo.Position, 
                    elementInfo.Size
                );
                
                // Update both the collection and UI
                if (index >= 0 && index < CanvasElements.Count)
                {
                    CanvasElements[index] = updatedInfo;
                    image.Tag = updatedInfo; // Update the UI element's Tag
                    System.Diagnostics.Debug.WriteLine($"Updated element at index {index} with action: {element.Action.ClassName}.{element.Action.MethodName}");
                }
            });

            await dialog.ShowAsync();
        }
    }
}
