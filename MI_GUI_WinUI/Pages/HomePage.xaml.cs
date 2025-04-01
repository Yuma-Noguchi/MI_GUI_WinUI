using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.UI.Xaml;
using MI_GUI_WinUI.ViewModels;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly INavigationService _navigationService;
        private readonly IWindowManager _windowManager;
        private readonly MainWindowViewModel _viewModel;

        public HomePage()
        {
            InitializeComponent();
            
            // Get dependencies from DI container
            _navigationService = Ioc.Default.GetService<INavigationService>();
            _windowManager = Ioc.Default.GetService<IWindowManager>();
            _viewModel = Ioc.Default.GetService<MainWindowViewModel>();
            
            // Set DataContext
            this.DataContext = _viewModel;
        }

        private void NavigateToSelectProfiles(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null && _navigationService != null)
            {
                _navigationService.Navigate<SelectProfilesPage, SelectProfilesViewModel>(window);
            }
        }

        private void NavigateToActionStudio(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null && _navigationService != null)
            {
                _navigationService.Navigate<ActionStudioPage, ActionStudioViewModel>(window);
            }
        }

        private void NavigateToIconStudio(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null && _navigationService != null)
            {
                _navigationService.Navigate<IconStudioPage, IconStudioViewModel>(window);
            }
        }

        private void NavigateToProfileEditor(object sender, RoutedEventArgs e)
        {
            var window = _windowManager?.MainWindow;
            if (window != null && _navigationService != null)
            {
                _navigationService.Navigate<ProfileEditorPage, ProfileEditorViewModel>(window);
            }
        }
    }
}