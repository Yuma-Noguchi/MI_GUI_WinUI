using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.ViewModels;
using System.Collections.Generic;
using System;
using Microsoft.UI;
using System.IO;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;

namespace MI_GUI_WinUI
{
    /// <summary>
    /// The main window of the application
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private INavigationService _navigationService;
        private readonly IPageFactory _pageFactory;
        private readonly Dictionary<string, (Window Window, DateTime LastActivated)> _activeWindows;
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(
            INavigationService navigationService,
            IPageFactory pageFactory,
            MainWindowViewModel viewModel)
        {
            this.InitializeComponent();

            _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _activeWindows = new Dictionary<string, (Window, DateTime)>();

            // Set the window icon
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(wndId);
            appWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "motioninputgames-logo.ico"));

            // Set DataContext
            if (Content is FrameworkElement element)
            {
                element.DataContext = _viewModel;
            }

            // Set navigation service if provided
            if (navigationService != null)
            {
                SetNavigationService(navigationService);
            }

            // Handle window closing
            this.Closed += MainWindow_Closed;
        }

        public void SetNavigationService(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // Initialize navigation if we have a ContentFrame
            if (Content is FrameworkElement element)
            {
                var frame = element.FindName("ContentFrame") as Frame;
                if (frame != null)
                {
                    _navigationService.Initialize(frame);
                    _navigationService.RegisterFrame(this, frame);

                    // Navigate to HomePage using PageFactory
                    var homePage = _pageFactory.CreatePage<HomePage>();
                    frame.Navigate(typeof(HomePage));
                }
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_navigationService != null)
            {
                _navigationService.UnregisterWindow(this);
            }
        }
    }
}
