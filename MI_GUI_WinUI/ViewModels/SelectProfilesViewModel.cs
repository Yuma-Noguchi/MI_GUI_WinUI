﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
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
                _logger.LogInformation($"Updated filtered profiles. Count: {_filteredProfiles.Count}");
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
                previews.Clear();
                _previewCache.Clear();

                // Load profiles
                await LoadProfilesAsync();
                
                // Generate previews after loading
                if (_profiles.Count > 0)
                {
                    await GenerateGuiElementsPreviewsAsync();
                    _logger.LogInformation($"Initialization complete. Profile count: {_profiles.Count}, Preview count: {previews.Count}");
                }
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
                _profiles = await _profileService.ReadProfilesFromJsonAsync(profilesFolderPath);
                _logger.LogInformation($"Loaded {_profiles?.Count ?? 0} profiles from {profilesFolderPath}");
                UpdateFilteredProfiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profiles");
                ErrorMessage = "Failed to load profiles. Please try again.";
                _profiles = new List<Profile>();
                _filteredProfiles = new List<Profile>();
            }
        }

        public async Task GenerateGuiElementsPreviewsAsync()
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

                // Remove any cached previews that no longer exist in filtered profiles
                var filteredProfileNames = _filteredProfiles.Select(p => p.Name).ToHashSet();
                var keysToRemove = _previewCache.Keys.Where(k => !filteredProfileNames.Contains(k)).ToList();
                foreach (var key in keysToRemove)
                {
                    _previewCache.Remove(key);
                }

                // Generate previews for filtered profiles
                foreach (var profile in _filteredProfiles)
                {
                    ProfilePreview? preview;
                    if (!_previewCache.TryGetValue(profile.Name, out preview))
                    {
                        preview = await GeneratePreviewForProfileAsync(profile);
                        if (preview != null)
                        {
                            _previewCache[profile.Name] = preview;
                        }
                    }

                    if (preview != null)
                    {
                        previews.Add(preview);
                    }
                }
                
                _logger.LogInformation($"Generated {previews.Count} previews");
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

                // Add GUI elements to preview
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

                // Add poses to preview
                if (profile.Poses != null)
                {
                    foreach (PoseGuiElement pose in profile.Poses)
                    {
                        try
                        {
                            string poseFilePath = Path.Combine(guiElementsFolderPath, pose.Skin);
                            // Create a GuiElement-like structure for loading the image
                            var poseAsGuiElement = new GuiElement
                            {
                                Position = pose.Position,
                                Radius = pose.Radius,
                                Skin = pose.Skin
                            };
                            
                            var image = await LoadImageAsync(poseFilePath, poseAsGuiElement);
                            
                            if (image != null)
                            {
                                // Scale positions and offset by scaled radius
                                Canvas.SetLeft(image, pose.Position[0] * SCALE_FACTOR - (pose.Radius * SCALE_FACTOR));
                                Canvas.SetTop(image, pose.Position[1] * SCALE_FACTOR - (pose.Radius * SCALE_FACTOR));
                                preview.Children.Add(image);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error loading image for pose {pose.File}");
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
                _logger.LogInformation($"Starting edit of profile: {profileName}");

                if (string.IsNullOrWhiteSpace(profileName))
                {
                    ErrorMessage = "Profile name is empty";
                    return;
                }

                // Load the full profile data
                var profile = GetProfileByName(profileName);
                if (profile == null)
                {
                    _logger.LogWarning($"Profile not found: {profileName}");
                    ErrorMessage = $"Profile '{profileName}' not found";
                    return;
                }

                // Log the profile data
                _logger.LogInformation($"Found profile with {profile?.GuiElements?.Count ?? 0} GUI elements and {profile?.Poses?.Count ?? 0} poses");

                // Navigate to editor with the profile
                _navigationService.Navigate<ProfileEditorPage>(profile);
                _logger.LogInformation($"Navigated to editor for profile: {profileName}");
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
                
                _logger.LogInformation($"Starting deletion of profile: {profileName}");
                
                // Remove from preview cache first
                _previewCache.Remove(profileName);

                // Delete the profile file first
                await _profileService.DeleteProfileAsync(profileName, profilesFolderPath);
                _logger.LogInformation($"Deleted profile file for: {profileName}");

                // Remove from in-memory list
                _profiles.RemoveAll(p => p.Name == profileName);
                _logger.LogInformation($"Removed profile from memory. Remaining profiles: {_profiles.Count}");

                // Update filtered profiles
                UpdateFilteredProfiles();
                
                // Generate new previews
                await GenerateGuiElementsPreviewsAsync();
                
                _logger.LogInformation($"Profile deletion completed. Preview count: {previews.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting profile: {profileName}");
                ErrorMessage = "Failed to delete profile.";
                
                // Try to reload profiles in case of error to ensure UI consistency
                try
                {
                    await LoadProfilesAsync();
                    await GenerateGuiElementsPreviewsAsync();
                }
                catch (Exception reloadEx)
                {
                    _logger.LogError(reloadEx, "Error reloading profiles after delete error");
                }
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
