using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.ViewModels.Base;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Service interface for application navigation with window and ViewModel lifecycle management
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Gets whether the navigation service can navigate backward
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Initializes the navigation service with a frame
        /// </summary>
        /// <param name="frame">The navigation frame to use</param>
        void Initialize(Frame frame);

        /// <summary>
        /// Registers a window with the navigation service
        /// </summary>
        /// <param name="window">The window to register</param>
        void RegisterWindow(Window window);

        /// <summary>
        /// Unregisters a window from the navigation service
        /// </summary>
        /// <param name="window">The window to unregister</param>
        void UnregisterWindow(Window window);

        /// <summary>
        /// Registers a frame with a specific window
        /// </summary>
        /// <param name="window">The window to associate with the frame</param>
        /// <param name="frame">The frame to register</param>
        void RegisterFrame(Window window, Frame frame);

        /// <summary>
        /// Navigates to a page
        /// </summary>
        /// <typeparam name="T">The type of page to navigate to</typeparam>
        /// <param name="parameter">Optional parameter to pass to the page</param>
        /// <returns>True if navigation was successful</returns>
        bool Navigate<T>(object? parameter = null) where T : Page;

        /// <summary>
        /// Navigates to a page with a specific ViewModel
        /// </summary>
        /// <typeparam name="TPage">The type of page to navigate to</typeparam>
        /// <typeparam name="TViewModel">The type of ViewModel to use</typeparam>
        /// <param name="window">The window to associate with the ViewModel</param>
        /// <param name="parameter">Parameter to pass to the page</param>
        /// <returns>True if navigation was successful</returns>
        bool Navigate<TPage, TViewModel>(Window window, object parameter = null) 
            where TPage : Page 
            where TViewModel : ViewModelBase;

        /// <summary>
        /// Navigates to a page with a specific ViewModel asynchronously
        /// </summary>
        /// <typeparam name="TPage">The type of page to navigate to</typeparam>
        /// <typeparam name="TViewModel">The type of ViewModel to use</typeparam>
        /// <param name="window">The window to associate with the ViewModel</param>
        /// <param name="parameter">Optional parameter to pass to the page</param>
        /// <returns>True if navigation was successful</returns>
        Task<bool> NavigateAsync<TPage, TViewModel>(Window window, object? parameter = null)
            where TPage : Page
            where TViewModel : ViewModelBase;

        /// <summary>
        /// Navigates backward one page in the navigation stack
        /// </summary>
        /// <returns>True if navigation was successful</returns>
        bool GoBack();

        /// <summary>
        /// Event raised when navigation occurs
        /// </summary>
        event EventHandler<string> NavigationChanged;
    }
}