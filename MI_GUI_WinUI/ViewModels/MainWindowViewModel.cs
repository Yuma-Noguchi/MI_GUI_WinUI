using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MI_GUI_WinUI.Pages;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private string _selectedMenuItem;

        [ObservableProperty]
        private string _title = "MotionInput Configuration";

        public string SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem, value))
                {
                    HandleNavigation(value);
                }
            }
        }

        public ICommand NavigateCommand { get; }

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateCommand = new RelayCommand<string>(HandleNavigation);
            
            // Subscribe to navigation changes
            _navigationService.NavigationChanged += (s, pageName) =>
            {
                SelectedMenuItem = pageName;
            };
        }

        private void HandleNavigation(string? pageName)
        {
            if (string.IsNullOrEmpty(pageName)) return;

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
            }
        }
    }
}