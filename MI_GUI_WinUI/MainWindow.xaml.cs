using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using CommunityToolkit.Mvvm.DependencyInjection;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Pages;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI
{
    /// <summary>
    /// Main window of the application implementing proper window management.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly MainWindowViewModel? _viewModel;
        private readonly INavigationService _navigationService;
        private readonly Dictionary<string, (Window Window, DateTime LastActivated)> _activeWindows;
        private const string WindowId = "MainWindow";

        public MainWindow()
        {
            this.InitializeComponent();
            
            _activeWindows = new Dictionary<string, (Window, DateTime)>();
            
            // Get services from DI
            _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            _viewModel = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        
            // Initialize navigation
            _navigationService.Initialize(ContentFrame, NavView);
            
            // Navigate to initial page
            _navigationService.Navigate<SelectProfilesPage>();

            // Register navigation service
            var services = Ioc.Default.GetService<IServiceCollection>();
            if (services != null)
            {
                services.AddSingleton(_navigationService);
            }

            // Restore window state
            WindowState.RestoreState(this, WindowId, 1280, 720);

            // Handle window closing
            this.Closed += MainWindow_Closed;

            // Navigate to default page
            _navigationService.Navigate<SelectProfilesPage>();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            WindowState.SaveState(this, WindowId);
        }

        private void CreateChildWindow<T>(string windowId, string title, T content) where T : UIElement
        {
            // Check if window is already open
            if (_activeWindows.TryGetValue(windowId, out var existing))
            {
                // Bring existing window to front
                var appWindow = GetAppWindowForWindow(existing.Window);
                if (appWindow != null)
                {
                    var presenter = appWindow.Presenter as OverlappedPresenter;
                    if (presenter?.State == OverlappedPresenterState.Minimized)
                    {
                        presenter.Restore();
                    }
                }
                existing.Window.Activate();
                _activeWindows[windowId] = (existing.Window, DateTime.Now);
                return;
            }

            // Create new window
            var newWindow = new Window();
            newWindow.Content = content;
            newWindow.Title = title;

            // Set up window
            var childAppWindow = GetAppWindowForWindow(newWindow);
            if (childAppWindow != null)
            {
                var presenter = childAppWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsResizable = true;
                    presenter.IsMaximizable = true;
                    presenter.IsMinimizable = true;
                }
            }

            // Store window reference
            _activeWindows[windowId] = (newWindow, DateTime.Now);

            // Handle window closing
            newWindow.Closed += (s, e) =>
            {
                _activeWindows.Remove(windowId);
                WindowState.SaveState(newWindow, windowId);
            };

            // Restore window state and show
            WindowState.RestoreState(newWindow, windowId);
            newWindow.Activate();
        }

        private static AppWindow? GetAppWindowForWindow(Window window)
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
