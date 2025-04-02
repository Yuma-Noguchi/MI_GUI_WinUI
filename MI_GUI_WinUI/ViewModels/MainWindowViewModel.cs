using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.ViewModels.Base;
using MI_GUI_WinUI.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _title = "MotionInput Configuration";

        [ObservableProperty]
        private string _selectedMenuItem;

        [ObservableProperty]
        private string? _errorMessage;

        public ICommand NavigateCommand { get; }

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            INavigationService navigationService)
            : base(logger, navigationService)
        {
            NavigateCommand = new RelayCommand<string>(HandleNavigation);
            
            // Subscribe to navigation changes
            _navigationService.NavigationChanged += OnNavigationChanged;
        }

        protected override void OnWindowChanged()
        {
            base.OnWindowChanged();
            if (Window != null)
            {
                // Initialize navigation when window is set
                HandleNavigation("SelectProfiles");
            }
        }

        private void OnNavigationChanged(object? sender, string pageName)
        {
            try
            {
                SelectedMenuItem = pageName;
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling navigation change");
                ErrorMessage = "Navigation error occurred";
            }
        }

        private void HandleNavigation(string? pageName)
        {
            if (string.IsNullOrEmpty(pageName)) return;

            try
            {
                switch (pageName)
                {
                    case "SelectProfiles":
                        _navigationService.Navigate<SelectProfilesPage>();
                        break;
                    case "ActionStudio":
                        _navigationService.Navigate<ActionStudioPage>();
                        break;
                    case "IconStudio":
                        _navigationService.Navigate<IconStudioPage>();
                        break;
                    case "ProfileEditor":
                        _navigationService.Navigate<ProfileEditorPage>();
                        break;
                    default:
                        _logger.LogWarning($"Unknown page name: {pageName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to {pageName}");
                ErrorMessage = $"Error navigating to {pageName}";
            }
        }

        public override void Cleanup()
        {
            try
            {
                // Unsubscribe from navigation events
                _navigationService.NavigationChanged -= OnNavigationChanged;

                base.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        protected override async Task ShowErrorAsync(string message)
        {
            ErrorMessage = message;
            await Task.CompletedTask;
        }
    }
}