using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using MI_GUI_WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class IconStudioPage : Page
    {
        private readonly IconStudioViewModel ViewModel;

        public IconStudioPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetService<IconStudioViewModel>();
            DataContext = ViewModel;
            this.Loaded += IconStudioPage_Loaded;
            this.Unloaded += Page_Unloaded;
        }

        private async void IconStudioPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PulseStoryboard != null)
            {
                PulseStoryboard.Begin();
            }

            // Ensure we have valid ViewModel and XamlRoot
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = this.XamlRoot;
                await ViewModel.InitializeAsync();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (PulseStoryboard != null)
            {
                PulseStoryboard.Stop();
            }

            //ViewModel?.Cleanup();

        }
    }
}