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
        private const float DROPPED_IMAGE_SIZE = 80;
        private readonly string PROFILES_DIR = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput", "data", "profiles");

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private XamlRoot? xamlRoot;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        partial void OnValidationMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasValidationMessage));
        }

        public ObservableCollection<UnifiedPositionInfo> CanvasElements { get; } = new();
        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();

        public ProfileEditorViewModel()
        {
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

        public void UpdateElementPosition(UnifiedPositionInfo info)
        {
            var existingElement = CanvasElements.FirstOrDefault(e => 
                e.Element.Position.SequenceEqual(info.Element.Position));

            if (existingElement != null)
            {
                var index = CanvasElements.IndexOf(existingElement);
                CanvasElements[index] = info;
            }
        }

        [RelayCommand]
        public void NewProfile()
        {
            ProfileName = string.Empty;
            ValidationMessage = string.Empty;
            CanvasElements.Clear();
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

        public async Task ConfigureAction(UnifiedPositionInfo elementInfo)
        {
            if (XamlRoot == null) return;

            var dialog = new ActionConfigurationDialog();
            dialog.XamlRoot = XamlRoot;
            var originalIndex = CanvasElements.IndexOf(elementInfo);
            if (originalIndex < 0)
            {
                // Fallback to position-based identification if index not found
                originalIndex = CanvasElements.ToList().FindIndex(e => 
                    Math.Abs(e.Position.X - elementInfo.Position.X) < 1 &&
                    Math.Abs(e.Position.Y - elementInfo.Position.Y) < 1);
            }

            dialog.Configure(elementInfo.Element, element => 
            {
                // Create updated element with the same position and size
                var updatedInfo = new UnifiedPositionInfo(
                    element,
                    elementInfo.Position, 
                    elementInfo.Size
                );
                
                // Update at the saved index
                if (originalIndex >= 0 && originalIndex < CanvasElements.Count)
                {
                    CanvasElements[originalIndex] = updatedInfo;
                    System.Diagnostics.Debug.WriteLine($"Updated element at index {originalIndex} with action: {element.Action.ClassName}.{element.Action.MethodName}");
                }
            });

            await dialog.ShowAsync();
        }
    }
}
