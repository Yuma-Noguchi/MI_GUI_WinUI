using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
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
    public sealed partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly Dictionary<string, (Window Window, DateTime LastActivated)> _activeWindows;
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Set the window icon
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(wndId);
            appWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "motioninputgames-logo.ico"));

            _activeWindows = new Dictionary<string, (Window, DateTime)>();
            
            // Get services from DI
            _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            _viewModel = Ioc.Default.GetRequiredService<MainWindowViewModel>();
            
            // Register this window with navigation service
            _navigationService.RegisterWindow(this);
            
            // Set DataContext
            if (Content is FrameworkElement element)
            {
                element.DataContext = _viewModel;
            }

            // Initialize navigation
            _navigationService.Initialize(ContentFrame);
            
            // Register frame with navigation service
            _navigationService.RegisterFrame(this, ContentFrame);

            // Create HomePage through DI - no need to manually initialize
            var homePage = Ioc.Default.GetRequiredService<HomePage>();
            ContentFrame.Navigate(typeof(HomePage));

            // Handle window closing
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _navigationService.UnregisterWindow(this);
        }
    }
}
