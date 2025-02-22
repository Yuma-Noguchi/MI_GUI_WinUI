using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.Storage;

namespace MI_GUI_WinUI.Services;

/// <summary>
/// Manages window lifecycle and state for the application.
/// </summary>
public class WindowManager
{
    private readonly Dictionary<Guid, Window> _windows;
    private Window _mainWindow;

    public WindowManager()
    {
        _windows = new Dictionary<Guid, Window>();
    }

    public Window MainWindow => _mainWindow;

    public void InitializeMainWindow()
    {
        _mainWindow = new MainWindow();
        var windowId = Guid.NewGuid();
        _windows[windowId] = _mainWindow;
        _mainWindow.Closed += (sender, args) => OnWindowClosed(windowId);
        ActivateWindow(_mainWindow);
    }

    public void ActivateWindow(Window window)
    {
        if (window != null)
        {
            // Restore window state if available
            RestoreWindowState(window);
            window.Activate();
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

            // If main window is closed, exit the application
            if (window == _mainWindow)
            {
                _mainWindow = null;
                Application.Current.Exit();
            }
        }
    }

    private void SaveWindowState(Window window)
    {
        if (window?.AppWindow != null)
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
        }
    }

    private void RestoreWindowState(Window window)
    {
        if (window?.AppWindow != null)
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
            }
        }
    }
}