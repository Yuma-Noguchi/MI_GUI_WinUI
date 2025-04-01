using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MI_GUI_WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MI_GUI_WinUI.Services.Interfaces;
using System;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ActionStudioPage : Page
    {
        private readonly ActionStudioViewModel ViewModel;
        private readonly IWindowManager _windowManager;

        public ActionStudioPage()
        {
            this.InitializeComponent();
            var services = App.Current.Services;
            ViewModel = services.GetRequiredService<ActionStudioViewModel>();
            _windowManager = services.GetRequiredService<IWindowManager>();
            DataContext = ViewModel;
            this.Loaded += ActionStudioPage_Loaded;
            this.Unloaded += ActionStudioPage_Unloaded;
        }

        private async void ActionStudioPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Set Window for dialogs
                ViewModel.Window = _windowManager.MainWindow;
                
                // Begin initialization
                await ViewModel.InitializeAsync();
            }
        }

        private void ActionStudioPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Window = null;
                ViewModel.Cleanup();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = XamlRoot;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (ViewModel != null)
            {
                ViewModel?.Cleanup();
            }
        }
    }
}
