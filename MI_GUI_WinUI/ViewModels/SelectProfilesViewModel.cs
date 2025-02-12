﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Storage;

namespace MI_GUI_WinUI.ViewModels;

public partial class SelectProfilesViewModel : ObservableObject, INotifyPropertyChanged
{
    [ObservableProperty]
    private string _selectedProfile;

    [ObservableProperty]
    private bool _isPopupOpen;

    [ObservableProperty]
    private ProfilePreview _selectedProfilePreview;

    private Window _window;
    public Window Window
    {
        get => _window;
        set
        {
            _window = value;
            _appWindow = _window.AppWindow;
        }
    }

    private AppWindow _appWindow;

    private List<Profile> _profiles;

    private Profile? GetProfileByName(string name)
    {
        return _profiles?.FirstOrDefault(p => p.Name == name);
    }

    public class ProfilePreview
    {
        public Canvas Canvas { get; set; }
        public string ProfileName { get; set; }

        public ProfilePreview Clone()
        {
            return new ProfilePreview
            {
                ProfileName = this.ProfileName,
                Canvas = CloneCanvas(this.Canvas)
            };
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

    public SelectProfilesViewModel(ProfileService profileService)
    {
        System.Diagnostics.Debug.WriteLine("Initializing SelectProfilesViewModel");
        _profileService = profileService;
        _profiles = _profileService.ReadProfileFromJson(profilesFolderPath);
        System.Diagnostics.Debug.WriteLine($"Loaded {_profiles?.Count ?? 0} profiles from {profilesFolderPath}");
    }

    public async void GenerateGuiElementsPreview()
    {
        System.Diagnostics.Debug.WriteLine("Starting GenerateGuiElementsPreview");
        previews.Clear();
        System.Diagnostics.Debug.WriteLine($"Total profiles to process: {_profiles?.Count ?? 0}");

        if (_profiles == null || _profiles.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("No profiles found to generate previews from");
            return;
        }

        foreach (Profile profile in _profiles)
        {
            System.Diagnostics.Debug.WriteLine($"Creating preview with {profile.GuiElements?.Count ?? 0} GUI elements");
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
                    
                        Image image = new Image
                        {
                            Source = new BitmapImage(new Uri(guiElementFilePath)),
                            Width = guiElement.Radius,
                            Height = guiElement.Radius,
                            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform
                        };
                        
                        Canvas.SetLeft(image, guiElement.Position[0] - guiElement.Radius);
                        Canvas.SetTop(image, guiElement.Position[1] - guiElement.Radius);

                        preview.Children.Add(image);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                        continue;
                    }
                }
            }
            var profilePreview = new ProfilePreview
            {
                Canvas = preview,
                ProfileName = profile.Name
            };
            previews.Add(profilePreview);
        }
    }

    public void EditProfile(string profileName)
    {
        var profileIndex = _profiles.FindIndex(p => p.Name == profileName);
        if (profileIndex >= 0)
        {
            var window = new Window();
            var profileEditor = new ProfileEditor(_profiles[profileIndex]);
            window.Content = profileEditor;
            window.Activate();
        }
    }

    public void DeleteProfile(string profileName)
    {
        var profileIndex = _profiles.FindIndex(p => p.Name == profileName);
        if (profileIndex >= 0)
        {
            _profiles.RemoveAt(profileIndex);
            _profileService.SaveProfilesToJson(_profiles, profilesFolderPath);
            GenerateGuiElementsPreview();
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
