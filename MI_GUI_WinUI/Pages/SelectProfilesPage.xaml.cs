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

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class SelectProfilesPage : Page
    {
        private readonly SelectProfilesViewModel ViewModel;

        public SelectProfilesPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetService<SelectProfilesViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (ViewModel != null)
                {
                    // Initialize profiles (preview generation is handled inside)
                    await ViewModel.InitializeAsync();
                }
                else
                {
                    throw new InvalidOperationException("ViewModel is not initialized.");
                }
            }
            catch (Exception ex)
            {
                // Log error and show error message
                if (ViewModel != null)
                {
                    ViewModel.ErrorMessage = $"Error initializing page: {ex.Message}";
                }
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // Clean up event handlers and references
            if (ViewModel != null)
            {
                ViewModel.IsPopupOpen = false;
                ViewModel.SelectedProfilePreview = null;
            }
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
