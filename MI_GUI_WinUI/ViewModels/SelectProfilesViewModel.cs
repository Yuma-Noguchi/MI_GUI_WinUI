﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class SelectProfilesViewModel : ObservableObject
    {
        private readonly ILogger<SelectProfilesViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly IMotionInputService _motionInputService;
        private readonly IProfileService _profileService;
        private readonly Dictionary<string, ProfilePreview> _previewCache = new();
        private readonly SemaphoreSlim _previewLock = new(1, 1);

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

        public class ProfilePreview
        {
            public required Canvas Canvas { get; set; }
            public required string ProfileName { get; set; }

            private ProfilePreview() { }

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
                        Image clonedImage = new Image
                        {
                            Source = originalImage.Source,
                            Width = originalImage.Width * popupScaleFactor,
                            Height = originalImage.Height * popupScaleFactor,
                            Stretch = originalImage.Stretch
                        };

                        Canvas.SetLeft(clonedImage, Canvas.GetLeft(originalImage) * popupScaleFactor);
                        Canvas.SetTop(clonedImage, Canvas.GetTop(originalImage) * popupScaleFactor);

                        clone.Children.Add(clonedImage);
                    }
                }

                return clone;
            }
        }

        public ObservableCollection<ProfilePreview> previews { get; } = new ObservableCollection<ProfilePreview>();

        private const double GAME_HEIGHT = 480.0;
        private const double GAME_WIDTH = 640.0;
        private const double TARGET_HEIGHT = 240.0;
        private const double TARGET_WIDTH = (TARGET_HEIGHT * GAME_WIDTH) / GAME_HEIGHT;
        private const double SCALE_FACTOR = TARGET_HEIGHT / GAME_HEIGHT;

        private string profilesFolderPath = "MotionInput\\data\\profiles";
        private readonly string guiElementsFolderPath = "MotionInput\\data\\assets";

        public SelectProfilesViewModel(
            IProfileService profileService,
            ILogger<SelectProfilesViewModel> logger,
            INavigationService navigationService,
            IMotionInputService motionInputService)
        {
            _logger = logger;
            _profileService = profileService;
            _navigationService = navigationService;
            _motionInputService = motionInputService;
            _profiles = new List<Profile>();

            InitializeAsync();
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

        public async Task GenerateGuiElementsPreviewsAsync(bool skipLock = false)
        {
            if (!skipLock)
                await _previewLock.WaitAsync();
            try
            {
                ErrorMessage = null;
                _previewCache.Clear();
                previews.Clear();

                if (_filteredProfiles == null || _filteredProfiles.Count == 0)
                {
                    _logger.LogInformation("No profiles to generate previews from");
                    return;
                }

                foreach (var profile in _filteredProfiles)
                {
                    var preview = await GeneratePreviewForProfileAsync(profile);
                    if (preview != null)
                    {
                        _previewCache[profile.Name] = preview;
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
            finally
            {
                if (!skipLock)
                    _previewLock.Release();
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
                            string guiElementFilePath = Path.Combine(guiElementsFolderPath.Replace('\\', '/'), guiElement.Skin?.Replace('\\', '/') ?? string.Empty).Replace('/', '\\');
                            var image = await LoadImageAsync(guiElementFilePath, guiElement);

                            if (image != null && guiElement.Position.Count >= 2)
                            {
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

                if (profile.Poses != null)
                {
                    foreach (PoseGuiElement pose in profile.Poses)
                    {
                        try
                        {
                            string poseFilePath = Path.Combine(guiElementsFolderPath, pose.Skin?.Replace('/', '\\') ?? string.Empty);
                            _logger.LogInformation($"Loading pose from: {poseFilePath}");
                            var image = await LoadImageAsync(poseFilePath, new GuiElement 
                            {
                                Position = pose.Position,
                                Radius = pose.Radius,
                                Skin = pose.Skin
                            });

                            if (image != null && pose.Position.Count >= 2)
                            {
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
                if (string.IsNullOrEmpty(element.Skin))
                {
                    _logger.LogWarning($"Skipping image load - empty skin path for element");
                    return null;
                }

                _logger.LogInformation($"Loading image from: {imagePath}");
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFile imageFile = await installedLocation.GetFileAsync(imagePath);
                if (imageFile == null)
                {
                    _logger.LogWarning($"Image file not found: {imagePath}");
                    return null;
                }

                var bitmap = new BitmapImage();
                using (IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.Read))
                {
                    await bitmap.SetSourceAsync(stream);
                }

                double scaledSize = element.Radius * 2 * SCALE_FACTOR;
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
            await _previewLock.WaitAsync();
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                
                _logger.LogInformation($"Starting deletion of profile: {profileName}");

                // Clear all caches first
                _previewCache.Clear();
                previews.Clear();

                // Delete the profile file
                await _profileService.DeleteProfileAsync(profileName, profilesFolderPath);
                _logger.LogInformation($"Deleted profile file for: {profileName}");

                // Reload all profiles fresh
                await LoadProfilesAsync();
                _logger.LogInformation($"Reloaded profiles. Count: {_profiles.Count}");

                // Generate fresh previews - skip lock since we already have it
                await GenerateGuiElementsPreviewsAsync(skipLock: true);
                _logger.LogInformation($"Regenerated previews. Count: {previews.Count}");
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
                _previewLock.Release();
            }
        }

        [RelayCommand]
        private async Task SelectProfileAsync()
        {
            if (SelectedProfilePreview == null) return;

            try
            {
                var profileName = SelectedProfilePreview.ProfileName.Replace(" ", "_");
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
                
                // Keep paths relative to installation directory
                string sourcePath = Path.Combine(profilesFolderPath, $"{profileName}.json");
                string destPath = Path.Combine("MotionInput\\data\\modes", $"{profileName}.json");
                
                _logger.LogInformation($"Attempting to copy from '{sourcePath}' to '{destPath}'");
                
                StorageFile sourceFile = await installedLocation.GetFileAsync(sourcePath);
                StorageFile destFile = await installedLocation.CreateFileAsync(destPath, CreationCollisionOption.ReplaceExisting);

                await sourceFile.CopyAndReplaceAsync(destFile);
                _logger.LogInformation($"Copied profile from {sourcePath} to {destPath}");

                bool success = await _motionInputService.StartAsync(profileName);

                if (success)
                {
                    await ClosePopupAsync();
                }
                else
                {
                    ErrorMessage = "Failed to launch MotionInput.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting profile");
                ErrorMessage = "Error launching MotionInput.";
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

