using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels.Base;
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
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ViewModelBase
    {
        private readonly IProfileService _profileService;
        private readonly ActionConfigurationDialog _actionConfigurationDialog;
        private readonly HeadTiltConfigurationDialog _headTiltConfigurationDialog;
        private const float DROPPED_IMAGE_SIZE = 80;
        private const int MOTION_INPUT_WIDTH = 640;
        private const int MOTION_INPUT_HEIGHT = 480;
        private const int CANVAS_WIDTH = 560;
        private const int CANVAS_HEIGHT = 420;
        private readonly string PROFILES_DIR = Path.Combine("MotionInput", "data", "profiles");

        [ObservableProperty]
        private string profileName = string.Empty;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private XamlRoot? xamlRoot;

        [ObservableProperty]
        private bool shouldClearCanvas;

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        public ObservableCollection<UnifiedPositionInfo> CanvasElements { get; } = new();
        public ObservableCollection<EditorButton> DefaultButtons { get; } = new();
        public ObservableCollection<EditorButton> CustomButtons { get; } = new();

        private IEnumerable<EditorButton> GetAvailableButtons()
        {
            return DefaultButtons.Concat(CustomButtons).ToList();
        }

        public ProfileEditorViewModel(
            IProfileService profileService,
            ActionConfigurationDialog actionConfigurationDialog,
            HeadTiltConfigurationDialog headTiltConfigurationDialog,
            ILogger<ProfileEditorViewModel> logger,
            INavigationService navigationService)
            : base(logger, navigationService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _actionConfigurationDialog = actionConfigurationDialog ?? throw new ArgumentNullException(nameof(actionConfigurationDialog));
            _headTiltConfigurationDialog = headTiltConfigurationDialog ?? throw new ArgumentNullException(nameof(headTiltConfigurationDialog));

            InitializeDefaultButtons();
            LoadCustomButtons();
        }

        private PoseGuiElement GetHeadTiltConfiguration()
        {
            var existingConfig = CanvasElements
                .FirstOrDefault(e => e.Element.File == "head_tilt_joystick.py");

            if (existingConfig != null && existingConfig.Element != null)
            {
                var element = existingConfig.Element.ToPoseElement();
                
                // Ensure position is set
                if (element.Position == null || element.Position.Count != 2)
                {
                    var scaledPosition = ScaleToMotionInput(existingConfig.Position);
                    element.Position = new List<int> { 
                        (int)scaledPosition.X, 
                        (int)scaledPosition.Y 
                    };
                }
                return element;
            }

            // Create new configuration with defaults
            return new PoseGuiElement
            {
                File = string.Empty,  // Empty file indicates disabled state
                Position = new List<int> { 
                    MOTION_INPUT_WIDTH / 2, 
                    MOTION_INPUT_HEIGHT / 2 
                },
                Sensitivity = 0.75,
                Deadzone = 1.0,
                Linear = false,
                LeftSkin = "racing/left_arrow.png",
                RightSkin = "racing/right_arrow.png"
            };
        }

        [RelayCommand]
        private async Task ConfigureHeadTilt()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (XamlRoot == null)
                {
                    throw new InvalidOperationException("XamlRoot is not available");
                }

                _headTiltConfigurationDialog.XamlRoot = XamlRoot;
                var poseElement = GetHeadTiltConfiguration();
                
                _headTiltConfigurationDialog.Configure(
                    poseElement,
                    GetAvailableButtons(),
                    UpdateHeadTiltElement
                );

                await _headTiltConfigurationDialog.ShowAsync();
            }, nameof(ConfigureHeadTilt));
        }

        private void UpdateHeadTiltElement(PoseGuiElement headTiltElement)
        {
            try
            {
                if (string.IsNullOrEmpty(headTiltElement.File))
                {
                    var existingElement = CanvasElements
                        .FirstOrDefault(e => e.Element.File == "head_tilt_joystick.py");
                    
                    if (existingElement != null)
                    {
                        CanvasElements.Remove(existingElement);
                    }
                    return;
                }

                var existing = CanvasElements
                    .FirstOrDefault(e => e.Element.File == "head_tilt_joystick.py");

                Point canvasPosition;
                if (existing != null)
                {
                    canvasPosition = new Point(
                        existing.Position.X,
                        existing.Position.Y
                    );
                }
                else
                {
                    canvasPosition = new Point(CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
                }

                var element = UnifiedGuiElement.FromPoseElement(headTiltElement);
                var elementInfo = new UnifiedPositionInfo(
                    element,
                    canvasPosition,
                    new Size(DROPPED_IMAGE_SIZE, DROPPED_IMAGE_SIZE)
                );

                var oldElement = CanvasElements.FirstOrDefault(e => e.Element.File == "head_tilt_joystick.py");
                if (oldElement != null)
                {
                    CanvasElements.Remove(oldElement);
                }

                CanvasElements.Add(elementInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating head tilt element");
                ValidationMessage = "Error updating head tilt configuration";
            }
        }

        protected override void OnWindowChanged()
        {
            base.OnWindowChanged();
            if (Window?.Content?.XamlRoot != null)
            {
                XamlRoot = Window.Content.XamlRoot;
            }
            else
            {
                XamlRoot = null;
            }
        }

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

        partial void OnValidationMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasValidationMessage));
        }

        private void InitializeDefaultButtons()
        {
            try
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
                };

                DefaultButtons.Clear();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default buttons");
                ValidationMessage = "Error loading default buttons";
            }
        }

        private void LoadCustomButtons()
        {
            try
            {
                var iconsPath = Path.Combine(
                    Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                    "MotionInput", "data", "assets", "generated_icons"
                );

                CustomButtons.Clear();
                if (Directory.Exists(iconsPath))
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading custom buttons");
                ValidationMessage = "Error loading custom buttons";
            }
        }

        private void PrepareForEdit()
        {
            CanvasElements.Clear();
            ValidationMessage = string.Empty;
        }

        public async Task LoadExistingProfile(Profile profile)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
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
                    // Look for head tilt configuration first
                    var headTilt = profile.Poses.FirstOrDefault(p => p.File == "head_tilt_joystick.py");
                    
                    // Load head tilt first if present
                    if (headTilt != null)
                    {
                        var unifiedElement = UnifiedGuiElement.FromPoseElement(headTilt);
                        // Head tilt is always centered in the canvas
                        var canvasPosition = new Point(CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
                        AddElementToCanvas(ElementAddRequest.FromExisting(
                            unifiedElement,
                            canvasPosition
                        ));
    
                        // Configure the dialog with loaded settings
                        _headTiltConfigurationDialog.Configure(headTilt, GetAvailableButtons(), UpdateHeadTiltElement);
                    }

                    // Load other poses
                    foreach (var pose in profile.Poses)
                    {
                        // Skip head tilt as it's already handled
                        if (pose.File == "head_tilt_joystick.py")
                            continue;

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
                else
                {
                    // If no poses section, ensure head tilt is disabled
                    var emptyHeadTilt = new PoseGuiElement
                    {
                        File = string.Empty,
                        Position = new List<int> { MOTION_INPUT_WIDTH / 2, MOTION_INPUT_HEIGHT / 2 }
                    };
                    var dummyRequest = ElementAddRequest.FromExisting(
                        UnifiedGuiElement.FromPoseElement(emptyHeadTilt),
                        new Point(CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2)
                    );
                    _headTiltConfigurationDialog.Configure(emptyHeadTilt, GetAvailableButtons(), UpdateHeadTiltElement);
                }

                ValidationMessage = "Profile loaded successfully";
            }, nameof(LoadExistingProfile));
        }

        [RelayCommand]
        public void AddButtonToCanvas(UnifiedPositionInfo buttonInfo)
        {
            try
            {
                CanvasElements.Add(buttonInfo);
                ValidationMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding button to canvas");
                ValidationMessage = "Error adding button";
            }
        }

        public void AddElementToCanvas(ElementAddRequest request)
        {
            try
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
                ValidationMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding element to canvas");
                ValidationMessage = "Error adding element";
            }
        }

        public void UpdateElementPosition(UnifiedPositionInfo info, int index)
        {
            try
            {
                if (index >= 0 && index < CanvasElements.Count)
                {
                    CanvasElements[index] = info;
                    ValidationMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating element position");
                ValidationMessage = "Error updating position";
            }
        }

        [RelayCommand]
        public void NewProfile()
        {
            try
            {
                ShouldClearCanvas = true;
                ProfileName = string.Empty;
                ValidationMessage = string.Empty;
                CanvasElements.Clear();

                // Initialize head tilt in disabled state
                var emptyHeadTilt = new PoseGuiElement
                {
                    File = string.Empty,
                    Position = new List<int> { MOTION_INPUT_WIDTH / 2, MOTION_INPUT_HEIGHT / 2 },
                    LeftSkin = "racing/left_arrow.png",
                    RightSkin = "racing/right_arrow.png",
                    Sensitivity = 0.75,
                    Deadzone = 1,
                    Linear = false
                };

                // Configure dialog with disabled state to ensure consistency
                if (_headTiltConfigurationDialog != null)
                {
                    _headTiltConfigurationDialog.Configure(emptyHeadTilt, GetAvailableButtons(), UpdateHeadTiltElement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new profile");
                ValidationMessage = "Error creating profile";
            }
        }

        [RelayCommand]
        public void ClearCanvas()
        {
            try
            {
                CanvasElements.Clear();
                ValidationMessage = string.Empty;

                // Reset head tilt to disabled state
                var emptyHeadTilt = new PoseGuiElement
                {
                    File = string.Empty,
                    Position = new List<int> { MOTION_INPUT_WIDTH / 2, MOTION_INPUT_HEIGHT / 2 },
                    LeftSkin = "racing/left_arrow.png",
                    RightSkin = "racing/right_arrow.png",
                    Sensitivity = 0.75,
                    Deadzone = 1,
                    Linear = false
                };

                // Configure dialog with disabled state
                if (_headTiltConfigurationDialog != null)
                {
                    _headTiltConfigurationDialog.Configure(emptyHeadTilt, GetAvailableButtons(), UpdateHeadTiltElement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing canvas");
                ValidationMessage = "Error clearing canvas";
            }
        }

        [RelayCommand]
        public async Task SaveProfile()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    throw new InvalidOperationException("Please enter a profile name");
                }

                if (!Utils.FileNameHelper.IsValidFileName(ProfileName))
                {
                    throw new InvalidOperationException("The profile name contains invalid characters");
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

                    // Handle head tilt elements differently
                    if (elementWithPosition.File == "head_tilt_joystick.py")
                    {
                        profile.Poses.Add(elementWithPosition.ToPoseElement());
                    }
                    // Handle other pose elements
                    else if (elementWithPosition.IsPose)
                    {
                        var poseElement = elementWithPosition.ToPoseElement();
                        poseElement.Position = new List<int> {
                            (int)scaledPosition.X,
                            (int)scaledPosition.Y
                        };
                        profile.Poses.Add(poseElement);
                    }
                    else
                    {
                        var guiElement = elementWithPosition.ToGuiElement();
                        guiElement.Position = new List<int> {
                            (int)scaledPosition.X,
                            (int)scaledPosition.Y
                        };
                        profile.GuiElements.Add(guiElement);
                    }
                }

                await _profileService.SaveProfileAsync(profile, PROFILES_DIR);
                ValidationMessage = "Profile saved successfully";

                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowMessage(
                        $"Profile saved as {sanitizedName}.json",
                        "Success",
                        XamlRoot
                    );
                }
            }, nameof(SaveProfile));
        }

        [RelayCommand]
        public async Task LoadProfile()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                ShouldClearCanvas = true;
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    throw new InvalidOperationException("Please enter a profile name to load");
                }

                var profiles = await _profileService.ReadProfilesFromJsonAsync(PROFILES_DIR);
                var profile = profiles.FirstOrDefault(p => p.Name == ProfileName);

                if (profile.Equals(default(Profile)))
                {
                    throw new InvalidOperationException("Profile not found");
                }

                await LoadExistingProfile(profile);
            }, nameof(LoadProfile));
        }

        public async Task ConfigureAction(UnifiedPositionInfo elementInfo, ResizableImage image)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (XamlRoot == null)
                {
                    throw new InvalidOperationException("XamlRoot is not available");
                }

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
            }, nameof(ConfigureAction));
        }

        protected override async Task ShowErrorAsync(string message)
        {
            ValidationMessage = message;
            if (XamlRoot != null)
            {
                await Utils.DialogHelper.ShowError(message, XamlRoot);
            }
        }

        public override void Cleanup()
        {
            try
            {
                CanvasElements.Clear();
                DefaultButtons.Clear();
                CustomButtons.Clear();
                XamlRoot = null;
                ValidationMessage = string.Empty;

                base.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }
    }
}
