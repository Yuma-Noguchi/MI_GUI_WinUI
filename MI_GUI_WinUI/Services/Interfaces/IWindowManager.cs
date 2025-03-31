using Microsoft.UI.Xaml;
using System;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Interface for managing window lifecycle and state
    /// </summary>
    public interface IWindowManager
    {
        /// <summary>
        /// Gets the main window instance
        /// </summary>
        Window MainWindow { get; }

        /// <summary>
        /// Initializes the main application window
        /// </summary>
        void InitializeMainWindow();

        /// <summary>
        /// Activates a window and restores its state
        /// </summary>
        /// <param name="window">The window to activate</param>
        void ActivateWindow(Window window);

        /// <summary>
        /// Creates and activates a new window of the specified type
        /// </summary>
        /// <typeparam name="T">The type of window to create</typeparam>
        /// <returns>The created window instance</returns>
        T CreateWindow<T>() where T : Window, new();

        /// <summary>
        /// Gets a window by its ID
        /// </summary>
        /// <param name="windowId">The window's unique identifier</param>
        /// <returns>The window instance if found, null otherwise</returns>
        Window GetWindow(Guid windowId);

        /// <summary>
        /// Closes a window by its ID
        /// </summary>
        /// <param name="windowId">The window's unique identifier</param>
        void CloseWindow(Guid windowId);
    }
}