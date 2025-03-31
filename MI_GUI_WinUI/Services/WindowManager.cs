using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services.Interfaces;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Manages window lifecycle and state for the application.
    /// </summary>
    public class WindowManager : IWindowManager
    {
        private readonly Dictionary<Guid, Window> _windows;
        private readonly ILogger<WindowManager> _logger;
        private Window _mainWindow;

        public WindowManager(ILogger<WindowManager> logger)
        {
            _logger = logger;
            _windows = new Dictionary<Guid, Window>();
        }

        public Window MainWindow => _mainWindow;

        public void InitializeMainWindow()
        {
            _logger.LogInformation("Initializing main window");
            _mainWindow = new MainWindow();
            var windowId = Guid.NewGuid();
            _windows[windowId] = _mainWindow;
            _mainWindow.Closed += (sender, args) => OnWindowClosed(windowId);
            ActivateWindow(_mainWindow);
        }

        public T CreateWindow<T>() where T : Window, new()
        {
            _logger.LogInformation("Creating new window of type: {type}", typeof(T).Name);
            var window = new T();
            var windowId = Guid.NewGuid();
            _windows[windowId] = window;
            window.Closed += (sender, args) => OnWindowClosed(windowId);
            ActivateWindow(window);
            return window;
        }

        public void ActivateWindow(Window window)
        {
            if (window != null)
            {
                // Restore window state if available
                RestoreWindowState(window);
                window.Activate();
                _logger.LogDebug("Window activated: {id}", _windows.FirstOrDefault(x => x.Value == window).Key);
            }
            else
            {
                _logger.LogWarning("Attempted to activate null window");
            }
        }

        public Window GetWindow(Guid windowId)
        {
            if (_windows.TryGetValue(windowId, out var window))
            {
                return window;
            }
            _logger.LogWarning("Window not found: {id}", windowId);
            return null;
        }

        public void CloseWindow(Guid windowId)
        {
            if (_windows.TryGetValue(windowId, out var window))
            {
                _logger.LogInformation("Closing window: {id}", windowId);
                SaveWindowState(window);
                _windows.Remove(windowId);
                window.Close();
            }
            else
            {
                _logger.LogWarning("Attempted to close non-existent window: {id}", windowId);
            }
        }

        private void OnWindowClosed(Guid windowId)
        {
            if (_windows.TryGetValue(windowId, out var window))
            {
                // Save window state before closing
                SaveWindowState(window);
                _windows.Remove(windowId);
                
                // Cleanup
                window.Closed -= (sender, args) => OnWindowClosed(windowId);
                window = null;

                _logger.LogInformation("Window closed and cleaned up: {id}", windowId);

                // If main window is closed, exit the application
                if (window == _mainWindow)
                {
                    _logger.LogInformation("Main window closed, exiting application");
                    _mainWindow = null;
                    Application.Current.Exit();
                }
            }
        }

        private void SaveWindowState(Window window)
        {
            if (window?.AppWindow != null)
            {
                try
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    var key = _windows.First(x => x.Value == window).Key;
                    
                    // Save window position and size
                    var position = window.AppWindow.Position;
                    var size = window.AppWindow.Size;
                    
                    localSettings.Values[$"Window_{key}_PosX"] = position.X;
                    localSettings.Values[$"Window_{key}_PosY"] = position.Y;
                    localSettings.Values[$"Window_{key}_Width"] = size.Width;
                    localSettings.Values[$"Window_{key}_Height"] = size.Height;

                    _logger.LogDebug("Window state saved: {id}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving window state");
                }
            }
        }

        private void RestoreWindowState(Window window)
        {
            if (window?.AppWindow != null)
            {
                try
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    var key = _windows.First(x => x.Value == window).Key;

                    // Restore window position and size if available
                    if (localSettings.Values.TryGetValue($"Window_{key}_PosX", out var posX) &&
                        localSettings.Values.TryGetValue($"Window_{key}_PosY", out var posY) &&
                        localSettings.Values.TryGetValue($"Window_{key}_Width", out var width) &&
                        localSettings.Values.TryGetValue($"Window_{key}_Height", out var height))
                    {
                        window.AppWindow.Move(new PointInt32((int)posX, (int)posY));
                        window.AppWindow.Resize(new SizeInt32((int)width, (int)height));
                        _logger.LogDebug("Window state restored: {id}", key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restoring window state");
                }
            }
        }
    }
}