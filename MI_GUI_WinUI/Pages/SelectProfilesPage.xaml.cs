using System;
using MI_GUI_WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using MI_GUI_WinUI;
using MI_GUI_WinUI.Services;
using Microsoft.Extensions.DependencyInjection;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MI_GUI_WinUI.Pages
{
    /// <summary>
    /// A page for selecting Motion Input profiles.
    /// </summary>
    public sealed partial class SelectProfilesPage : Page
    {
        private SelectProfilesViewModel? _viewModel;

        public SelectProfilesPage()
        {
            this.InitializeComponent();
            
            // Get ViewModel from DI if not provided through navigation
            if (_viewModel == null)
            {
                _viewModel = Ioc.Default.GetRequiredService<SelectProfilesViewModel>();
                SetViewModel(_viewModel);
            }
        }

        private void SetViewModel(SelectProfilesViewModel viewModel)
        {
            _viewModel = viewModel;
            if (Application.Current is App app)
            {
                var windowManager = app.Services.GetRequiredService<WindowManager>();
                _viewModel.Window = windowManager.MainWindow;
            }
            this.DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                // If we got ViewModel through navigation, use it
                if (e.Parameter is SelectProfilesViewModel vm)
                {
                    SetViewModel(vm);
                }

                // Ensure window reference is set
                if (_viewModel != null && Application.Current is App app)
                {
                    var windowManager = app.Services.GetRequiredService<WindowManager>();
                    _viewModel.Window = windowManager.MainWindow;
                }

                if (_viewModel != null)
                {
                    // Initialize profiles (preview generation is handled inside)
                    await _viewModel.InitializeAsync();
                }
                else
                {
                    throw new InvalidOperationException("ViewModel is not initialized.");
                }
            }
            catch (Exception ex)
            {
                // Log error and show error message
                if (_viewModel != null)
                {
                    _viewModel.ErrorMessage = $"Error initializing page: {ex.Message}";
                }
            }
        }

        public SelectProfilesViewModel? ViewModel
        {
            get => _viewModel;
            private set
            {
                if (_viewModel != value)
                {
                    _viewModel = value;
                    this.DataContext = _viewModel;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // Clean up event handlers and references
            if (ViewModel != null)
            {
                ViewModel.IsPopupOpen = false;
                ViewModel.SelectedProfilePreview = null;
            }
            ViewModel = null;
        }

        private async void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string profileName)
            {
                await ViewModel.EditProfileAsync(profileName);
            }
        }

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string profileName)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Delete Profile",
                    Content = $"Are you sure you want to delete {profileName}?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await ViewModel.DeleteProfileAsync(profileName);
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Help();
        }

        private async void OpenProfilePopup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.CommandParameter is SelectProfilesViewModel.ProfilePreview preview &&
                ViewModel != null)
            {
                await ViewModel.OpenPopupAsync(preview);
            }
        }

        private async void BackToList_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.ClosePopupAsync();
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // Ensure popup is closed when navigating away
            if (ViewModel != null)
            {
                await ViewModel.ClosePopupAsync();
            }
        }
    }
}
