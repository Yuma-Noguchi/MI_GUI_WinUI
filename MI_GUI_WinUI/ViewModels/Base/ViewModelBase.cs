using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.ViewModels.Base
{
    /// <summary>
    /// Base class for all ViewModels providing common functionality
    /// </summary>
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        protected readonly ILogger _logger;
        protected readonly INavigationService _navigationService;
        private WeakReference<Window> _windowReference;
        private bool _disposed = false;

        protected ViewModelBase(ILogger logger, INavigationService navigationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        /// <summary>
        /// Gets or sets the Window associated with this ViewModel
        /// </summary>
        public Window Window
        {
            get
            {
                if (_windowReference != null && _windowReference.TryGetTarget(out var window))
                {
                    return window;
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    _windowReference = new WeakReference<Window>(value);
                }
                else
                {
                    _windowReference = null;
                }
                OnWindowChanged();
            }
        }

        /// <summary>
        /// Called when the Window property changes
        /// </summary>
        protected virtual void OnWindowChanged() { }

        /// <summary>
        /// Initializes the ViewModel asynchronously
        /// </summary>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public virtual void Cleanup()
        {
            _logger?.LogDebug("Base cleanup");
            _windowReference = null;
            _logger?.LogInformation($"Cleaned up ViewModel: {GetType().Name}");
        }

        /// <summary>
        /// Executes an action with error handling and logging
        /// </summary>
        protected async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string actionName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {ActionName}: {Message}", actionName, ex.Message);
                await ShowErrorAsync($"Error in {actionName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows an error message to the user
        /// </summary>
        protected virtual Task ShowErrorAsync(string message)
        {
            _logger.LogError("Error: {Message}", message);
            // Derived classes should implement their own error display logic
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Cleanup();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in Dispose");
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}