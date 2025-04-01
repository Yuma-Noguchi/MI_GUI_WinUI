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
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading;
using Microsoft.UI.Dispatching;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class SelectProfilesViewModel : ViewModelBase
    {
        private readonly IMotionInputService _motionInputService;
        private readonly IProfileService _profileService;
        private readonly Dictionary<string, ProfilePreview> _previewCache = new();
        private readonly SemaphoreSlim _previewLock = new(1, 1);
        private AppWindow? _appWindow;

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

        private List<Profile> _profiles = new();
        private List<Profile> _filteredProfiles = new();

        protected override void OnWindowChanged()
        {
            base.OnWindowChanged();
            _appWindow = Window?.AppWindow;
        }

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
                return Clone(TARGET_WIDTH, TARGET_HEIGHT);
            }

            public ProfilePreview Clone(double targetWidth, double targetHeight)
            {
                return ProfilePreview.Create(CloneCanvas(this.Canvas, targetWidth, targetHeight), this.ProfileName);
            }

            private static Canvas CloneCanvas(Canvas original, double targetWidth, double targetHeight)
            {
                // Calculate the scale factor based on the target dimensions
                double scaleFactorX = targetWidth / GAME_WIDTH;
                double scaleFactorY = targetHeight / GAME_HEIGHT;
                
                Canvas clone = new Canvas
                {
                    Width = targetWidth,
                    Height = targetHeight,
                    Background = original.Background,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                foreach (UIElement child in original.Children)
                {
                    if (child is Image originalImage)
                    {
                        double originalX, originalY;
                        
                        // Get the current position, or default to centered if not specified
                        if (double.IsNaN(Canvas.GetLeft(originalImage)))
                        {
                            originalX = GAME_WIDTH / 2;
                        }
                        else
                        {
                            // Convert from scaled position back to original game coordinates
                            originalX = Canvas.GetLeft(originalImage) / SCALE_FACTOR + (originalImage.Width / (2 * SCALE_FACTOR));
                        }
                        
                        if (double.IsNaN(Canvas.GetTop(originalImage)))
                        {
                            originalY = GAME_HEIGHT / 2;
                        }
                        else
                        {
                            // Convert from scaled position back to original game coordinates
                            originalY = Canvas.GetTop(originalImage) / SCALE_FACTOR + (originalImage.Height / (2 * SCALE_FACTOR));
                        }

                        // Create new image with scaled dimensions
                        Image clonedImage = new Image
                        {
                            Source = originalImage.Source,
                            Width = originalImage.Width / SCALE_FACTOR * scaleFactorX,
                            Height = originalImage.Height / SCALE_FACTOR * scaleFactorY,
                            Stretch = originalImage.Stretch
                        };

                        // Position the element in the new canvas, accounting for the element's size
                        Canvas.SetLeft(clonedImage, (originalX * scaleFactorX) - (clonedImage.Width / 2));
                        Canvas.SetTop(clonedImage, (originalY * scaleFactorY) - (clonedImage.Height / 2));

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
            : base(logger, navigationService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _motionInputService = motionInputService ?? throw new ArgumentNullException(nameof(motionInputService));
            _profiles = new List<Profile>();
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await LoadProfilesAsync();
            if (_profiles.Count > 0)
            {
                await GenerateGuiElementsPreviewsAsync();
            }
        }

        public override void Cleanup()
        {
            try
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();

                if (dispatcher != null)
                {
                    SafelyClearProfiles();
                }
                else if (Window != null && Window.DispatcherQueue != null)
                {
                    Window.DispatcherQueue.TryEnqueue(() =>
                    {
                        SafelyClearProfiles();
                    });
                }

                _previewCache.Clear();
                _previewLock.Dispose();
                foreach (var preview in previews)
                {
                    preview.Canvas.Children.Clear();
                }
                previews.Clear();

                base.Cleanup();
                _logger.LogInformation("SelectProfilesViewModel cleaned up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up SelectProfilesViewModel");
            }
        }

        private void SafelyClearProfiles()
        {
            try
            {
                if (previews != null)
                {
                    previews.Clear();
                }

                _profiles?.Clear();
                _filteredProfiles?.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing profile collections");
            }
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

        public async Task ClosePopupAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                IsPopupOpen = false;
                SelectedProfilePreview = null;
                ErrorMessage = null;
            }, nameof(ClosePopupAsync));
        }

        public void HandleBackNavigation()
        {
            if (IsPopupOpen)
            {
                _ = ClosePopupAsync();
            }
        }

        public async Task OpenPopupAsync(ProfilePreview preview)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                await ClosePopupAsync();

                SelectedProfilePreview = preview.Clone(560, 400);
                IsPopupOpen = true;
            }, nameof(OpenPopupAsync));
        }

        private async Task LoadProfilesAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                IsLoading = true;
                _profiles = await _profileService.ReadProfilesFromJsonAsync(profilesFolderPath);
                _logger.LogInformation($"Loaded {_profiles?.Count ?? 0} profiles from {profilesFolderPath}");
                UpdateFilteredProfiles();
            }, nameof(LoadProfilesAsync));
            IsLoading = false;
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
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation($"Starting edit of profile: {profileName}");

                if (string.IsNullOrWhiteSpace(profileName))
                {
                    ErrorMessage = "Profile name is empty";
                    return;
                }

                var profile = GetProfileByName(profileName);
                if (profile == null)
                {
                    _logger.LogWarning($"Profile not found: {profileName}");
                    ErrorMessage = $"Profile '{profileName}' not found";
                    return;
                }

                _logger.LogInformation($"Found profile with {profile?.GuiElements?.Count ?? 0} GUI elements and {profile?.Poses?.Count ?? 0} poses");
                _navigationService.Navigate<ProfileEditorPage>(profile);
                _logger.LogInformation($"Navigated to editor for profile: {profileName}");
            }, nameof(EditProfileAsync));
        }

        public async Task DeleteProfileAsync(string profileName)
        {
            await _previewLock.WaitAsync();
            try
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    _logger.LogInformation($"Starting deletion of profile: {profileName}");

                    _previewCache.Clear();
                    previews.Clear();

                    await _profileService.DeleteProfileAsync(profileName, profilesFolderPath);
                    _logger.LogInformation($"Deleted profile file for: {profileName}");

                    await LoadProfilesAsync();
                    _logger.LogInformation($"Reloaded profiles. Count: {_profiles.Count}");

                    await GenerateGuiElementsPreviewsAsync(skipLock: true);
                    _logger.LogInformation($"Regenerated previews. Count: {previews.Count}");
                }, nameof(DeleteProfileAsync));
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

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var profileName = SelectedProfilePreview.ProfileName.Replace(" ", "_");
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;

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
            }, nameof(SelectProfileAsync));
        }

        protected override async Task ShowErrorAsync(string message)
        {
            ErrorMessage = message;
            await Task.CompletedTask;
        }
    }
}
