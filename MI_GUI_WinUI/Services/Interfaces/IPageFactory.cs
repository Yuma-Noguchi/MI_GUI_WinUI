using Microsoft.UI.Xaml.Controls;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Provides a factory pattern for creating pages with their associated ViewModels
    /// </summary>
    public interface IPageFactory
    {
        /// <summary>
        /// Creates a page instance with its dependencies injected
        /// </summary>
        /// <typeparam name="TPage">The type of page to create</typeparam>
        /// <returns>An initialized page instance</returns>
        TPage CreatePage<TPage>() where TPage : Page;

        /// <summary>
        /// Creates a page instance with its associated ViewModel
        /// </summary>
        /// <typeparam name="TPage">The type of page to create</typeparam>
        /// <typeparam name="TViewModel">The type of ViewModel to associate</typeparam>
        /// <returns>An initialized page instance with ViewModel set</returns>
        TPage CreatePage<TPage, TViewModel>() 
            where TPage : Page
            where TViewModel : class;
    }
}