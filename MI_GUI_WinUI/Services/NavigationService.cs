using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.ViewModels.Base;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Implementation of INavigationService for WinUI navigation with window management
    /// </summary>
    public class NavigationService : INavigationService
    {
        private Frame? _frame;
        private readonly ILogger<NavigationService> _logger;
        private readonly ILoggingService _loggingService;
        private readonly IPageFactory _pageFactory;
        private readonly Dictionary<Window, WeakReference<ViewModelBase>> _windowViewModels;
        private readonly Dictionary<Window, Frame> _windowFrames;

        public event EventHandler<string> NavigationChanged;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public NavigationService(
            ILogger<NavigationService> logger,
            ILoggingService loggingService,
            IPageFactory pageFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            _windowViewModels = new Dictionary<Window, WeakReference<ViewModelBase>>();
            _windowFrames = new Dictionary<Window, Frame>();
        }

        public void Initialize(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _frame.Navigated += Frame_Navigated;
            _logger.LogInformation("Navigation service initialized");
        }

        public void RegisterWindow(Window window)
        {
            if (!_windowViewModels.ContainsKey(window))
            {
                _windowViewModels.Add(window, new WeakReference<ViewModelBase>(null));
                _logger.LogInformation("Window registered with navigation service");
            }

            // Find the main content frame
            if (window.Content is FrameworkElement element)
            {
                var frame = FindFrameInElement(element);
                if (frame != null)
                {
                    RegisterFrame(window, frame);
                }
            }
        }

        public void UnregisterWindow(Window window)
        {
            if (_windowViewModels.TryGetValue(window, out var viewModelRef))
            {
                if (viewModelRef.TryGetTarget(out var viewModel))
                {
                    viewModel.Cleanup();
                }
                _windowViewModels.Remove(window);
                _logger.LogInformation("Window unregistered from navigation service");
            }

            if (_windowFrames.ContainsKey(window))
            {
                _windowFrames.Remove(window);
            }
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
                // Get current page's ViewModel before navigation
                if (_frame.Content is FrameworkElement oldPage && 
                    oldPage.DataContext is ViewModelBase oldViewModel)
                {
                    // Dispose the old ViewModel
                    oldViewModel.Dispose();
                }

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

        public bool Navigate<TPage, TViewModel>(Window window, object parameter = null)
            where TPage : Page
            where TViewModel : ViewModelBase
        {
            try
            {
                // Create the page with its ViewModel using PageFactory
                var page = _pageFactory.CreatePage<TPage, TViewModel>();
                
                if (page.DataContext is TViewModel viewModel)
                {
                    // Set window reference
                    viewModel.Window = window;

                    // Initialize ViewModel if needed
                    _ = viewModel.InitializeAsync();

                    // Get the frame for this window
                    if (TryGetFrameForWindow(window, out var frame))
                    {
                        return frame.Navigate(page.GetType(), parameter);
                    }
                }
                else
                {
                    _logger.LogError("ViewModel not correctly set on page");
                }

                _logger.LogError("Navigation failed: No frame found for window");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation failed to {Page}", typeof(TPage).Name);
                return false;
            }
        }

        public async Task<bool> NavigateAsync<TPage, TViewModel>(Window window, object? parameter = null)
            where TPage : Page
            where TViewModel : ViewModelBase
        {
            if (_frame == null)
            {
                _logger.LogError("Cannot navigate: Frame is not initialized");
                return false;
            }

            try
            {
                // Create the page with its ViewModel using PageFactory
                var page = _pageFactory.CreatePage<TPage, TViewModel>();
                
                if (!(page.DataContext is TViewModel viewModel))
                {
                    _logger.LogError("ViewModel not correctly set on page");
                    return false;
                }

                // Set window reference and initialize
                viewModel.Window = window;
                await viewModel.InitializeAsync();

                // Store ViewModel reference
                if (_windowViewModels.ContainsKey(window))
                {
                    if (_windowViewModels[window].TryGetTarget(out var oldViewModel))
                    {
                        oldViewModel.Cleanup();
                    }
                    _windowViewModels[window] = new WeakReference<ViewModelBase>(viewModel);
                }
                else
                {
                    _windowViewModels.Add(window, new WeakReference<ViewModelBase>(viewModel));
                }

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

        public void RegisterFrame(Window window, Frame frame)
        {
            if (window != null && frame != null)
            {
                _windowFrames[window] = frame;
            }
        }

        private bool TryGetFrameForWindow(Window window, out Frame frame)
        {
            if (window != null && _windowFrames.TryGetValue(window, out frame))
            {
                return true;
            }
            
            frame = null;
            return false;
        }

        private Frame FindFrameInElement(FrameworkElement element)
        {
            // Try to find a Frame in the visual tree
            if (element is Frame frame)
            {
                return frame;
            }
            
            // Look for a content frame property or field by convention
            var contentFrame = element.FindName("ContentFrame") as Frame;
            if (contentFrame != null)
            {
                return contentFrame;
            }
            
            return null;
        }
    }
}