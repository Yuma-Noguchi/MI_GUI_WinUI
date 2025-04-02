using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Services.Interfaces;
using System;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Default implementation of IPageFactory that creates pages and their associated ViewModels
    /// using dependency injection
    /// </summary>
    public class PageFactory : IPageFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PageFactory> _logger;

        public PageFactory(
            IServiceProvider serviceProvider,
            ILogger<PageFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public TPage CreatePage<TPage>() where TPage : Page
        {
            try
            {
                _logger.LogDebug("Creating page of type {PageType}", typeof(TPage).Name);
                return ActivatorUtilities.CreateInstance<TPage>(_serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create page of type {PageType}", typeof(TPage).Name);
                throw new InvalidOperationException($"Failed to create page of type {typeof(TPage).Name}", ex);
            }
        }

        /// <inheritdoc/>
        public TPage CreatePage<TPage, TViewModel>()
            where TPage : Page
            where TViewModel : class
        {
            try
            {
                _logger.LogDebug("Creating page {PageType} with ViewModel {ViewModelType}", 
                    typeof(TPage).Name, typeof(TViewModel).Name);

                // Create the page first
                var page = CreatePage<TPage>();

                // Get the ViewModel from DI
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                // Set the DataContext
                page.DataContext = viewModel;

                _logger.LogDebug("Successfully created page {PageType} with ViewModel {ViewModelType}",
                    typeof(TPage).Name, typeof(TViewModel).Name);

                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create page {PageType} with ViewModel {ViewModelType}",
                    typeof(TPage).Name, typeof(TViewModel).Name);
                throw new InvalidOperationException(
                    $"Failed to create page {typeof(TPage).Name} with ViewModel {typeof(TViewModel).Name}", ex);
            }
        }
    }
}