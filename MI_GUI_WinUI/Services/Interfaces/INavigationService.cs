using System;
using Microsoft.UI.Xaml.Controls;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Service interface for application navigation
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
        /// <param name="parameter">Optional parameter to pass to the page</param>
        /// <returns>True if navigation was successful</returns>
        bool Navigate<TPage, TViewModel>(object? parameter = null)
            where TPage : Page
            where TViewModel : class;

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