using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using MI_GUI_WinUI.Pages;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace MI_GUI_WinUI.Services
{
    public interface INavigationService
    {
        bool CanGoBack { get; }
        void Initialize(Frame frame);
        bool Navigate<T>(object? parameter = null) where T : Page;
        bool Navigate<TPage, TViewModel>(object? parameter = null)
            where TPage : Page
            where TViewModel : class;
        bool GoBack();
        event EventHandler<string> NavigationChanged;
    }

    public class NavigationService : INavigationService
    {
        private Frame? _frame;

        public event EventHandler<string> NavigationChanged;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public void Initialize(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _frame.Navigated += Frame_Navigated;
        }

        public bool Navigate<T>(object? parameter = null) where T : Page
        {
            if (_frame == null) return false;

            var pageType = typeof(T);
            var result = _frame.Navigate(pageType, parameter);
            if (result)
            {
                NavigationChanged?.Invoke(this, pageType.Name);
            }
            return result;
        }

        public bool Navigate<TPage, TViewModel>(object? parameter = null)
            where TPage : Page
            where TViewModel : class
        {
            if (_frame == null) return false;

            // Get ViewModel from DI
            var viewModel = Ioc.Default.GetRequiredService<TViewModel>();
            
            // Navigate with ViewModel as parameter
            var result = _frame.Navigate(typeof(TPage), viewModel);
            if (result)
            {
                NavigationChanged?.Invoke(this, typeof(TPage).Name);
            }
            return result;
        }

        public bool GoBack()
        {
            if (_frame?.CanGoBack ?? false)
            {
                _frame.GoBack();
                return true;
            }
            return false;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            NavigationChanged?.Invoke(this, e.SourcePageType.Name);
        }
    }
}