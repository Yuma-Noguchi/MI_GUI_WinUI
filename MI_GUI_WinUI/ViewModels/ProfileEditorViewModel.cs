using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Collections.Generic;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        private readonly ILogger<ProfileEditorViewModel> _logger;
        private readonly string _guiElementsFolderPath;
        private readonly string _profilesFolderPath;

        [ObservableProperty]
        private string _title = "Profile Editor";

        [ObservableProperty]
        private Profile? _currentProfile;

        [ObservableProperty]
        private string _profileName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DraggableGuiElement> _defaultButtons = new();

        [ObservableProperty]
        private ObservableCollection<DraggableGuiElement> _customButtons = new();

        [ObservableProperty]
        private ObservableCollection<DraggableGuiElement> _canvasElements = new();

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private Canvas? _previewCanvas;

        [ObservableProperty]
        private bool _showGrid;

        [ObservableProperty]
        private bool _snapToGrid = true;

        private Window? _window;
        public Window? Window
        {
            get => _window;
            set
            {
                _window = value;
                _appWindow = _window?.AppWindow;
            }
        }

        private AppWindow? _appWindow;

        public ProfileEditorViewModel(ILogger<ProfileEditorViewModel> logger)
        {
            _logger = logger;
            _guiElementsFolderPath = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "MotionInput", "data", "assets"
            );
            _profilesFolderPath = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "MotionInput", "data", "profiles"
            );

            SaveProfileCommand = new AsyncRelayCommand(SaveProfileAsync, CanSaveProfile);
            CanvasElements.CollectionChanged += (s, e) => SaveProfileCommand.NotifyCanExecuteChanged();
        }

        private bool CanSaveProfile() => 
            !string.IsNullOrWhiteSpace(ProfileName) && CanvasElements.Count > 0;

        public IAsyncRelayCommand SaveProfileCommand { get; }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Load button templates
                await LoadButtonTemplatesAsync();

                // Reset canvas state
                CanvasElements.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing ProfileEditor");
                ErrorMessage = "Failed to initialize editor.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadButtonTemplatesAsync()
        {
            try
            {
                // Clear existing collections
                DefaultButtons.Clear();
                CustomButtons.Clear();

                // Add default buttons
                var defaultButtons = new List<(string name, string file)>
                {
                    ("A", "button_a.png"),
                    ("B", "button_b.png"),
                    ("X", "button_x.png"),
                    ("Y", "button_y.png"),
                    ("LB", "button_lb.png"),
                    ("RB", "button_rb.png"),
                    ("LT", "button_lt.png"),
                    ("RT", "button_rt.png"),
                    ("Start", "button_start.png"),
                    ("Select", "button_select.png"),
                    ("DPad", "button_dpad.png")
                };

                foreach (var btn in defaultButtons)
                {
                    var guiElement = new GuiElement
                    {
                        File = btn.file,
                        Position = new List<int> { 0, 0 },
                        Radius = 50,
                        Skin = $"default_{btn.name.ToLower()}.png",
                        TriggeredSkin = $"default_{btn.name.ToLower()}_pressed.png",
                        Action = new ActionConfig
                        {
                            ClassName = "DefaultButtonAction",
                            MethodName = $"Press{btn.name}",
                            Arguments = new List<string>()
                        }
                    };

                    DefaultButtons.Add(new DraggableGuiElement(guiElement));
                    Debug.WriteLine($"Added default button: {btn.name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading button templates");
                ErrorMessage = "Failed to load button templates.";
            }
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProfileName))
                {
                    ErrorMessage = "Please enter a profile name.";
                    return;
                }

                var profile = new Profile
                {
                    Name = ProfileName,
                    GlobalConfig = new Dictionary<string, string>(),
                    GuiElements = new List<GuiElement>(),
                    Poses = new List<PoseConfig>(),
                    SpeechCommands = new Dictionary<string, SpeechCommand>()
                };

                // Add GUI elements from canvas
                foreach (var element in CanvasElements)
                {
                    profile.GuiElements.Add(element.ToGuiElement());
                }

                var filePath = Path.Combine(_profilesFolderPath, $"{ProfileName}.json");
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(profile, 
                    new Newtonsoft.Json.JsonSerializerSettings 
                    { 
                        Formatting = Newtonsoft.Json.Formatting.Indented 
                    });

                await File.WriteAllTextAsync(filePath, jsonString);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                ErrorMessage = "Failed to save profile.";
            }
        }

        public void AddCanvasElement(DraggableGuiElement element)
        {
            if (element != null)
            {
                // Create a new instance to avoid reference issues
                var newElement = element.Clone();
                CanvasElements.Add(newElement);
                SaveProfileCommand.NotifyCanExecuteChanged();
                Debug.WriteLine($"Added element to canvas: {newElement.DisplayName} at ({newElement.X}, {newElement.Y})");
            }
        }

        public void RemoveCanvasElement(DraggableGuiElement element)
        {
            CanvasElements.Remove(element);
        }

        internal void Help()
        {
            // TODO: Implement help functionality
        }

        public void Cleanup()
        {
            // Clear collections
            DefaultButtons.Clear();
            CustomButtons.Clear();
            CanvasElements.Clear();

            // Reset state
            CurrentProfile = null;
            ProfileName = string.Empty;
            ErrorMessage = null;
            IsLoading = false;
        }
    }
}
