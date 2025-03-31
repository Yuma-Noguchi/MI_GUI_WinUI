using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.Services.Interfaces;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Implementation of INavigationService for WinUI navigation
    /// </summary>
    public class NavigationService : INavigationService
    {
        private Frame? _frame;
        private readonly ILogger<NavigationService> _logger;
        private readonly ILoggingService _loggingService;

        public event EventHandler<string> NavigationChanged;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public NavigationService(ILogger<NavigationService> logger, ILoggingService loggingService)
        {
            _logger = logger;
            _loggingService = loggingService;
        }

        public void Initialize(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _frame.Navigated += Frame_Navigated;
            _logger.LogInformation("Navigation service initialized");
        }

        public bool Navigate<T>(object? parameter = null) where T : Page
        {
            if (_frame == null)
            {
                _logger.LogError("Cannot navigate: Frame is not initialized");
                return false;
            }

            try
            {
                var pageType = typeof(T);
                var result = _frame.Navigate(pageType, parameter);
                if (result)
                {
                    _logger.LogInformation("Navigated to page: {Page}", pageType.Name);
                    NavigationChanged?.Invoke(this, pageType.Name);
                }
                else
                {
                    _logger.LogWarning("Navigation failed to page: {Page}", pageType.Name);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to page: {Page}", typeof(T).Name);
                return false;
            }
        }

        public bool Navigate<TPage, TViewModel>(object? parameter = null)
            where TPage : Page
            where TViewModel : class
        {
            if (_frame == null)
            {
                _logger.LogError("Cannot navigate: Frame is not initialized");
                return false;
            }

            try
            {
                // Get ViewModel from DI
                var viewModel = Ioc.Default.GetRequiredService<TViewModel>();
                
                // Navigate with ViewModel as parameter
                var pageType = typeof(TPage);
                var result = _frame.Navigate(pageType, viewModel);
                if (result)
                {
                    _logger.LogInformation("Navigated to page: {Page} with ViewModel: {ViewModel}", 
                        pageType.Name, typeof(TViewModel).Name);
                    NavigationChanged?.Invoke(this, pageType.Name);
                }
                else
                {
                    _logger.LogWarning("Navigation failed to page: {Page} with ViewModel: {ViewModel}", 
                        pageType.Name, typeof(TViewModel).Name);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to page: {Page} with ViewModel: {ViewModel}", 
                    typeof(TPage).Name, typeof(TViewModel).Name);
                return false;
            }
        }

        public bool GoBack()
        {
            if (_frame?.CanGoBack ?? false)
            {
                try
                {
                    _frame.GoBack();
                    _logger.LogInformation("Navigated back");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error navigating back");
                    return false;
                }
            }
            _logger.LogInformation("Cannot navigate back: no back stack");
            return false;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            _logger.LogDebug("Frame navigated to: {Page}", e.SourcePageType.Name);
            NavigationChanged?.Invoke(this, e.SourcePageType.Name);
        }
    }
}