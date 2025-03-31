using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Controls;
using MI_GUI_WinUI.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly IProfileService _profileService;
        private readonly ActionConfigurationDialog _actionConfigurationDialog;
        private const float DROPPED_IMAGE_SIZE = 80;
        private const int MOTION_INPUT_WIDTH = 640;
        private const int MOTION_INPUT_HEIGHT = 480;
        private const int CANVAS_WIDTH = 560;
        private const int CANVAS_HEIGHT = 420;
        private readonly string PROFILES_DIR = Path.Combine("MotionInput", "data", "profiles");

        private Point ScaleToMotionInput(Point canvasPosition)
        {
            return new Point(
                canvasPosition.X * MOTION_INPUT_WIDTH / CANVAS_WIDTH,
                canvasPosition.Y * MOTION_INPUT_HEIGHT / CANVAS_HEIGHT
            );
        }

        private Point ScaleToCanvas(List<int> motionInputPosition)
        {
            return new Point(
                motionInputPosition[0] * CANVAS_WIDTH / MOTION_INPUT_WIDTH,
                motionInputPosition[1] * CANVAS_HEIGHT / MOTION_INPUT_HEIGHT
            );
        }

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

        public ProfileEditorViewModel(IProfileService profileService, ActionConfigurationDialog actionConfigurationDialog)
        {
            _profileService = profileService;
            _actionConfigurationDialog = actionConfigurationDialog;
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
                        var canvasPosition = ScaleToCanvas(element.Position);
                        AddElementToCanvas(ElementAddRequest.FromExisting(
                            unifiedElement,
                            canvasPosition
                        ));
                    }
                }

                // Load poses
                if (profile.Poses != null)
                {
                    foreach (var pose in profile.Poses)
                    {
                        if (!string.IsNullOrEmpty(pose.Skin))
                        {
                            var unifiedElement = UnifiedGuiElement.FromPoseElement(pose);
                            var canvasPosition = ScaleToCanvas(pose.Position);
                            AddElementToCanvas(ElementAddRequest.FromExisting(
                                unifiedElement,
                                canvasPosition
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

            if (request.Button?.FileName != null)
            {
                element = element.WithSkin(request.Button.FileName);
            }

            var info = new UnifiedPositionInfo(
                element,
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

                var sanitizedName = Utils.FileNameHelper.SanitizeFileName(ProfileName);
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
                    var scaledPosition = ScaleToMotionInput(element.Position);
                    var elementWithPosition = element.Element.WithPosition(
                        (int)scaledPosition.X,
                        (int)scaledPosition.Y
                    );

                    if (elementWithPosition.IsPose)
                    {
                        profile.Poses.Add(elementWithPosition.ToPoseElement());
                    }
                    else
                    {
                        profile.GuiElements.Add(elementWithPosition.ToGuiElement());
                    }
                }

                await _profileService.SaveProfileAsync(profile, PROFILES_DIR);

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

                var profiles = await _profileService.ReadProfilesFromJsonAsync(PROFILES_DIR);
                var profile = profiles.FirstOrDefault(p => p.Name == ProfileName);

                if (profile.Equals(default(Profile)))
                {
                    ValidationMessage = "Profile not found";
                    return;
                }


                await LoadExistingProfile(profile);
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Error loading profile: {ex.Message}";
            }
        }

        public async Task ConfigureAction(UnifiedPositionInfo elementInfo, ResizableImage image)
        {
            if (XamlRoot == null) return;

            _actionConfigurationDialog.XamlRoot = XamlRoot;
            var index = CanvasElements.IndexOf(elementInfo);

            _actionConfigurationDialog.Configure(elementInfo.Element, element => 
            {
                var scaledPosition = ScaleToMotionInput(elementInfo.Position);
                var updatedInfo = elementInfo.With(
                    element: element.WithPosition(
                        (int)scaledPosition.X,
                        (int)scaledPosition.Y
                    )
                );
                
                if (index >= 0 && index < CanvasElements.Count)
                {
                    CanvasElements[index] = updatedInfo;
                    image.Tag = updatedInfo;
                }
            });

            await _actionConfigurationDialog.ShowAsync();
        }
    }
}
