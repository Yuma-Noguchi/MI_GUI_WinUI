using System;
using Windows.Storage;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI;

namespace MI_GUI_WinUI.Models;

public class WindowState
{
    private const string SettingsKey = "WindowState_";

    public static void SaveState(Window window, string windowId)
    {
        try
        {
            var appWindow = GetAppWindowForWindow(window);
            if (appWindow != null)
            {
                var settings = ApplicationData.Current.LocalSettings;
                
                // Save position and size
                settings.Values[$"{SettingsKey}{windowId}_X"] = appWindow.Position.X;
                settings.Values[$"{SettingsKey}{windowId}_Y"] = appWindow.Position.Y;
                settings.Values[$"{SettingsKey}{windowId}_Width"] = appWindow.Size.Width;
                settings.Values[$"{SettingsKey}{windowId}_Height"] = appWindow.Size.Height;

                // Save presentation state
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    settings.Values[$"{SettingsKey}{windowId}_IsMaximized"] = presenter.State == OverlappedPresenterState.Maximized;
                    settings.Values[$"{SettingsKey}{windowId}_IsMinimized"] = presenter.State == OverlappedPresenterState.Minimized;
                }
            }
        }
        catch (Exception)
        {
            // Silently fail if we can't save state
        }
    }

    public static void RestoreState(Window window, string windowId, int defaultWidth = 1024, int defaultHeight = 768)
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            var appWindow = GetAppWindowForWindow(window);
            if (appWindow == null) return;

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                // Set basic presenter options
                presenter.IsResizable = true;
                presenter.IsMinimizable = true;
                presenter.IsMaximizable = true;

                // Restore window state if saved
                if (settings.Values.TryGetValue($"{SettingsKey}{windowId}_IsMaximized", out var isMaximized) &&
                    (bool)isMaximized)
                {
                    presenter.Maximize();
                    return; // Skip position/size restore for maximized windows
                }

                if (settings.Values.TryGetValue($"{SettingsKey}{windowId}_IsMinimized", out var isMinimized) &&
                    (bool)isMinimized)
                {
                    presenter.Minimize();
                    return; // Skip position/size restore for minimized windows
                }
            }

            // Restore size
            if (settings.Values.TryGetValue($"{SettingsKey}{windowId}_Width", out var width) &&
                settings.Values.TryGetValue($"{SettingsKey}{windowId}_Height", out var height))
            {
                appWindow.Resize(new SizeInt32(
                    Convert.ToInt32(width),
                    Convert.ToInt32(height)
                ));
            }
            else
            {
                // Use defaults if no saved state
                appWindow.Resize(new SizeInt32(defaultWidth, defaultHeight));
            }

            // Restore position (only if not maximized/minimized)
            if (settings.Values.TryGetValue($"{SettingsKey}{windowId}_X", out var x) &&
                settings.Values.TryGetValue($"{SettingsKey}{windowId}_Y", out var y))
            {
                int posX = Convert.ToInt32(x);
                int posY = Convert.ToInt32(y);

                // Ensure window is visible on current displays
                if (IsPositionVisible(posX, posY))
                {
                    appWindow.Move(new PointInt32(posX, posY));
                }
                else
                {
                    CenterWindow(window);
                }
            }
            else
            {
                // Center the window if no position is saved
                CenterWindow(window);
            }
        }
        catch (Exception)
        {
            // If restoration fails, use defaults
            try
            {
                var appWindow = GetAppWindowForWindow(window);
                if (appWindow != null)
                {
                    appWindow.Resize(new SizeInt32(defaultWidth, defaultHeight));
                    CenterWindow(window);
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't even set defaults
            }
        }
    }

    private static bool IsPositionVisible(int x, int y)
    {
        // Basic check to ensure the position is somewhat visible
        // Could be enhanced to check actual display bounds
        return x > -8000 && x < 8000 && y > -8000 && y < 8000;
    }

    private static void CenterWindow(Window window)
    {
        var appWindow = GetAppWindowForWindow(window);
        if (appWindow == null) return;

        var displayArea = DisplayArea.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                WinRT.Interop.WindowNative.GetWindowHandle(window)
            ), 
            DisplayAreaFallback.Primary
        );

        if (displayArea != null)
        {
            var centerX = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            var centerY = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(new PointInt32(centerX, centerY));
        }
    }

    private static AppWindow? GetAppWindowForWindow(Window window)
    {
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        return AppWindow.GetFromWindowId(windowId);
    }
}