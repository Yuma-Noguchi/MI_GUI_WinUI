using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Storage;
using Windows.Storage.Streams;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.ViewModels;

public partial class SelectProfilesViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly ILogger<SelectProfilesViewModel> _logger;
    private readonly Dictionary<string, ProfilePreview> _previewCache = new();

    [ObservableProperty]
    private string? _selectedProfile;

    [ObservableProperty]
    private bool _isPopupOpen = false;

    [ObservableProperty]
    private ProfilePreview? _selectedProfilePreview;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

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
    private List<Profile> _profiles = new();
    private List<Profile> _filteredProfiles = new();

    private Profile? GetProfileByName(string name)
    {
        return _profiles?.FirstOrDefault(p => p.Name == name);
    }

    private void UpdateFilteredProfiles()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                _filteredProfiles = _profiles.ToList();
            }
            else
            {
                _filteredProfiles = _profiles
                    .Where(p => p.Name != null && p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            GenerateGuiElementsPreviews();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating filtered profiles");
            _filteredProfiles = new List<Profile>();
            ErrorMessage = "Error filtering profiles.";
        }
    }

    public class ProfilePreview
    {
        public required Canvas Canvas { get; set; }
        public required string ProfileName { get; set; }

        private ProfilePreview() { } // Private constructor to enforce using the initialization syntax

        public static ProfilePreview Create(Canvas canvas, string profileName)
        {
            return new ProfilePreview
            {
                Canvas = canvas,
                ProfileName = profileName
            };
        }

        public ProfilePreview Clone()
        {
            return ProfilePreview.Create(CloneCanvas(this.Canvas), this.ProfileName);
        }

        private static Canvas CloneCanvas(Canvas original)
        {
            Canvas clone = new Canvas
            {
                Width = original.Width,
                Height = original.Height,
                Background = original.Background,
                HorizontalAlignment = original.HorizontalAlignment
            };

            foreach (UIElement child in original.Children)
            {
                if (child is Image originalImage)
                {
                    Image clonedImage = new Image
                    {
                        Source = originalImage.Source,
                        Width = originalImage.Width,
                        Height = originalImage.Height,
                        Stretch = originalImage.Stretch
                    };

                    Canvas.SetLeft(clonedImage, Canvas.GetLeft(originalImage));
                    Canvas.SetTop(clonedImage, Canvas.GetTop(originalImage));

                    clone.Children.Add(clonedImage);
                }
            }

            return clone;
        }
    }

    public ObservableCollection<ProfilePreview> previews { get; } = new ObservableCollection<ProfilePreview>();

    private readonly ProfileService _profileService;

    private string profilesFolderPath = "MotionInput\\data\\profiles";

    private string guiElementsFolderPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput\\data\\assets");

    public SelectProfilesViewModel(ProfileService profileService, ILogger<SelectProfilesViewModel> logger)
    {
        _logger = logger;
        _profileService = profileService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            
            // Reset state
            IsPopupOpen = false;
            SelectedProfilePreview = null;
            ErrorMessage = null;

            // Load profiles if needed
            if (_profiles.Count == 0)
            {
                await LoadProfilesAsync();
            }
            
            // Generate previews after loading
            GenerateGuiElementsPreviews();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InitializeAsync");
            ErrorMessage = "Failed to initialize. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClosePopup()
    {
        try
        {
            IsPopupOpen = false;
            SelectedProfilePreview = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing popup");
            ErrorMessage = "Error closing popup.";
        }
    }

    public async Task ClosePopupAsync()
    {
        try
        {
            IsPopupOpen = false;
            SelectedProfilePreview = null;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing popup");
            ErrorMessage = "Error closing popup.";
        }
    }

    public void HandleBackNavigation()
    {
        if (IsPopupOpen)
        {
            ClosePopup();
        }
    }

    public async Task OpenPopupAsync(ProfilePreview preview)
    {
        try
        {
            // Reset any existing state
            ClosePopup();
                
            // Set new state
            SelectedProfilePreview = preview.Clone();
            IsPopupOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening popup");
            ErrorMessage = "Error opening profile preview.";
        }
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            _profiles = await _profileService.ReadProfilesFromJsonAsync(profilesFolderPath);
            _logger.LogInformation($"Loaded {_profiles?.Count ?? 0} profiles from {profilesFolderPath}");
            UpdateFilteredProfiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profiles");
            ErrorMessage = "Failed to load profiles. Please try again.";
            _profiles = new List<Profile>();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async void GenerateGuiElementsPreviews()
    {
        try
        {
            ErrorMessage = null;
            previews.Clear();

            if (_filteredProfiles == null || _filteredProfiles.Count == 0)
            {
                _logger.LogInformation("No profiles to generate previews from");
                return;
            }

            foreach (var profile in _filteredProfiles)
            {
                if (_previewCache.TryGetValue(profile.Name, out var cachedPreview))
                {
                    previews.Add(cachedPreview);
                    continue;
                }

                var preview = await GeneratePreviewForProfileAsync(profile);
                if (preview != null)
                {
                    _previewCache[profile.Name] = preview;
                    previews.Add(preview);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating previews");
            ErrorMessage = "Failed to generate profile previews.";
        }
    }

    private async Task<ProfilePreview?> GeneratePreviewForProfileAsync(Profile profile)
    {
        try
        {
            Canvas preview = new Canvas
            {
                Width = 640,
                Height = 480,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                HorizontalAlignment = HorizontalAlignment.Center,
                DataContext = new { ProfileName = profile.Name }
            };

            if (profile.GuiElements != null)
            {
                foreach (GuiElement guiElement in profile.GuiElements)
                {
                    try
                    {
                        string guiElementFilePath = Path.Combine(guiElementsFolderPath, guiElement.Skin);
                        var image = await LoadImageAsync(guiElementFilePath, guiElement);
                        
                        if (image != null)
                        {
                            Canvas.SetLeft(image, guiElement.Position[0] - guiElement.Radius);
                            Canvas.SetTop(image, guiElement.Position[1] - guiElement.Radius);
                            preview.Children.Add(image);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading image for {guiElement.Skin}");
                    }
                }
            }

            return ProfilePreview.Create(preview, profile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating preview for profile {profile.Name}");
            return null;
        }
    }

    private async Task<Image?> LoadImageAsync(string imagePath, GuiElement element)
    {
        try
        {
            var bitmap = new BitmapImage();
            using var fileStream = File.OpenRead(imagePath);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var randomAccessStream = memoryStream.AsRandomAccessStream();
            await bitmap.SetSourceAsync(randomAccessStream);

            return new Image
            {
                Source = bitmap,
                Width = element.Radius,
                Height = element.Radius,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load image: {imagePath}");
            return null;
        }
    }

    public async Task EditProfileAsync(string profileName)
    {
        try
        {
            var profile = GetProfileByName(profileName);
            if (profile != null)
            {
                //var window = new Window();
                //var profileEditor = new ProfileEditor(profile);
                //window.Content = profileEditor;
                //window.Activate();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error editing profile: {profileName}");
            ErrorMessage = "Failed to open profile editor.";
        }
    }

    public async Task DeleteProfileAsync(string profileName)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var profileIndex = _profiles.FindIndex(p => p.Name == profileName);
            if (profileIndex >= 0)
            {
                _profiles.RemoveAt(profileIndex);
                await _profileService.SaveProfilesToJsonAsync(_profiles, profilesFolderPath);
                _previewCache.Remove(profileName);
                UpdateFilteredProfiles();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting profile: {profileName}");
            ErrorMessage = "Failed to delete profile.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    internal void Home()
    {
        if (_window != null)
        {
            var mainWindow = new MainWindow();
            mainWindow.Activate();
            
            if (_window.AppWindow != null)
            {
                _window.AppWindow.Hide();
            }
        }
    }

    internal void Help()
    {
        // TODO: Implement help functionality
    }
}
