using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Pages;
using System.Collections.Generic;
using System;
using Microsoft.UI;
using System.IO;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace MI_GUI_WinUI
{
    public sealed partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly Dictionary<string, (Window Window, DateTime LastActivated)> _activeWindows;

        public MainWindow()
        {
            this.InitializeComponent();

            // Set the window icon
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(wndId);
            appWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets","motioninputgames-logo.ico"));

            _activeWindows = new Dictionary<string, (Window, DateTime)>();
            
            // Get the navigation service from DI
            _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            
            // Initialize navigation
            _navigationService.Initialize(ContentFrame);
            
            // Navigate to home page
            _navigationService.Navigate<HomePage>();
        }
    }
}
