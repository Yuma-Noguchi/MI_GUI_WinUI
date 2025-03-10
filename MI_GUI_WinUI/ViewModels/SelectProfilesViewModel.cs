﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class SelectProfilesViewModel : ObservableObject, INotifyPropertyChanged
    {
        private readonly ILogger<SelectProfilesViewModel> _logger;
        private readonly INavigationService _navigationService;
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

        public SelectProfilesViewModel(ProfileService profileService, ILogger<SelectProfilesViewModel> logger, INavigationService navigationService)
        {
            _logger = logger;
            _profileService = profileService;
            _navigationService = navigationService;
        }

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
                // For popup view, scale up to full game resolution
                double popupScaleFactor = GAME_HEIGHT / TARGET_HEIGHT;

                Canvas clone = new Canvas
                {
                    Width = GAME_WIDTH,
                    Height = GAME_HEIGHT,
                    Background = original.Background,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                foreach (UIElement child in original.Children)
                {
                    if (child is Image originalImage)
                    {
                        // Scale up the image for the popup view
                        Image clonedImage = new Image
                        {
                            Source = originalImage.Source,
                            Width = originalImage.Width * popupScaleFactor,
                            Height = originalImage.Height * popupScaleFactor,
                            Stretch = originalImage.Stretch
                        };

                        // Scale up the position for the popup view
                        Canvas.SetLeft(clonedImage, Canvas.GetLeft(originalImage) * popupScaleFactor);
                        Canvas.SetTop(clonedImage, Canvas.GetTop(originalImage) * popupScaleFactor);

                        clone.Children.Add(clonedImage);
                    }
                }

                return clone;
            }
        }

        public ObservableCollection<ProfilePreview> previews { get; } = new ObservableCollection<ProfilePreview>();

        private readonly ProfileService _profileService;
        private const double GAME_HEIGHT = 480.0; // Original game window height
        private const double GAME_WIDTH = 640.0;  // Original game window width
        private const double TARGET_HEIGHT = 240.0; // Preview canvas height
        private const double TARGET_WIDTH = (TARGET_HEIGHT * GAME_WIDTH) / GAME_HEIGHT;
        private const double SCALE_FACTOR = TARGET_HEIGHT / GAME_HEIGHT;

        private string profilesFolderPath = "MotionInput\\data\\profiles";

        private string guiElementsFolderPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "MotionInput\\data\\assets");

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
                    Width = TARGET_WIDTH,
                    Height = TARGET_HEIGHT,
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
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
                                // Scale positions and offset by scaled radius
                                Canvas.SetLeft(image, guiElement.Position[0] * SCALE_FACTOR - (guiElement.Radius * SCALE_FACTOR));
                                Canvas.SetTop(image, guiElement.Position[1] * SCALE_FACTOR - (guiElement.Radius * SCALE_FACTOR));
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

                double scaledSize = element.Radius * 2 * SCALE_FACTOR; // Diameter is 2 * radius
                return new Image
                {
                    Source = bitmap,
                    Width = scaledSize,
                    Height = scaledSize,
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
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    ErrorMessage = "Profile name is empty";
                    return;
                }

                var profile = GetProfileByName(profileName);
                if (profile == null)
                {
                    ErrorMessage = $"Profile '{profileName}' not found";
                    return;
                }

                _navigationService.Navigate<ProfileEditorPage, ProfileEditorViewModel>(profile);
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
}
