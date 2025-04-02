using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.UI.Xaml;
using System;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Pages;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    /// <summary>
    /// Main navigation page for the application
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private INavigationService _navigationService;
        private IWindowManager _windowManager;

        // Default constructor needed for XAML instantiation
        public HomePage()
        {
            InitializeComponent();
            
            // Get services after XAML initialization
            _navigationService = Ioc.Default.GetService<INavigationService>();
            _windowManager = Ioc.Default.GetService<IWindowManager>();
        }

        private void NavigateToSelectProfiles(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null)
            {
                _navigationService?.Navigate<SelectProfilesPage, SelectProfilesViewModel>(window);
            }
        }

        private void NavigateToActionStudio(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null)
            {
                _navigationService?.Navigate<ActionStudioPage, ActionStudioViewModel>(window);
            }
        }

        private void NavigateToIconStudio(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null)
            {
                _navigationService?.Navigate<IconStudioPage, IconStudioViewModel>(window);
            }
        }

        private void NavigateToProfileEditor(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null)
            {
                _navigationService?.Navigate<ProfileEditorPage, ProfileEditorViewModel>(window);
            }
        }
    }
}