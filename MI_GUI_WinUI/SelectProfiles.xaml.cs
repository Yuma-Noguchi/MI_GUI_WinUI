using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using CommunityToolkit.Mvvm.DependencyInjection;
using MI_GUI_WinUI.ViewModels;

namespace MI_GUI_WinUI;

/// <summary>
/// SelectProfiles page for managing profile selection and operations.
/// </summary>
public sealed partial class SelectProfiles : Page
{
    private Window parentWindow;
    
    public SelectProfiles()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetService<SelectProfilesViewModel>();
        DataContext = ViewModel;
        ViewModel.GenerateGuiElementsPreview();
    }

    public void Initialize(Window window)
    {
        parentWindow = window;
        ViewModel.Window = window;
    }

    public SelectProfilesViewModel ViewModel { get; }

    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string profileName)
        {
            ViewModel.EditProfile(profileName);
        }
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string profileName)
        {
            ViewModel.DeleteProfile(profileName);
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Help();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        if (parentWindow != null)
        {
            // Create and activate a new MainWindow
            var mainWindow = new MainWindow();
            mainWindow.Activate();

            // Hide current window
            if (parentWindow.AppWindow != null)
            {
                parentWindow.AppWindow.Hide();
            }
        }
    }

    private void SearchProfiles_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        //ViewModel.SearchProfiles(SearchProfiles.Text);
    }

    private void SearchProfiles_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        //ViewModel.SearchProfiles(args.QueryText);
    }

    private void SearchProfiles_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        //ViewModel.SearchProfiles(args.SelectedItem.ToString());
    }

    private void OpenProfilePopup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is SelectProfilesViewModel.ProfilePreview preview)
        {
            ViewModel.SelectedProfilePreview = preview.Clone();
            ViewModel.IsPopupOpen = true;
        }
    }

    private void SelectProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProfilePreview != null && parentWindow != null)
        {
            ViewModel.SelectedProfile = ViewModel.SelectedProfilePreview.ProfileName;
            parentWindow.AppWindow.Hide();
        }
    }

    private void BackToList_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupOpen = false;
        ViewModel.SelectedProfilePreview = null;
    }
}
