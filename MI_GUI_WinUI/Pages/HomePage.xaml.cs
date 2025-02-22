using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using MI_GUI_WinUI.Services;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly INavigationService _navigationService;

        public HomePage()
        {
            this.InitializeComponent();
            _navigationService = Ioc.Default.GetRequiredService<INavigationService>();

            // Wire up button click events
            IconStudioButton.Click += (s, e) => _navigationService.Navigate<IconStudioPage>();
            ActionStudioButton.Click += (s, e) => _navigationService.Navigate<ActionStudioPage>();
            SelectProfilesButton.Click += (s, e) => _navigationService.Navigate<SelectProfilesPage>();
            ProfileEditorButton.Click += (s, e) => _navigationService.Navigate<ProfileEditorPage>();
        }
    }
}