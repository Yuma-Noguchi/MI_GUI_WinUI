using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using MI_GUI_WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;

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
            // Start animations
            if (PulseStoryboard != null)
            {
                PulseStoryboard.Begin();
            }

             try
            {
                if (ViewModel != null)
                {
                    ViewModel.XamlRoot = XamlRoot;
                    // Initialize profiles (preview generation is handled inside)
                    await ViewModel.InitializeStableDiffusionAsync();
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (PulseStoryboard != null)
            {
                PulseStoryboard.Stop();
            }

            ViewModel?.Cleanup();
        }
    }
}
