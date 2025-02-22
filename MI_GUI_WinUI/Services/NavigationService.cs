using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using MI_GUI_WinUI.Pages;

namespace MI_GUI_WinUI.Services
{
    public interface INavigationService
    {
        bool CanGoBack { get; }
        void Initialize(Frame frame, NavigationView navigationView);
        bool Navigate<T>(object parameter = null) where T : Page;
        bool GoBack();
        event EventHandler<string> NavigationChanged;
    }

    public class NavigationService : INavigationService
    {
        private Frame? _frame;
        private NavigationView? _navigationView;

        public event EventHandler<string> NavigationChanged;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public void Initialize(Frame frame, NavigationView navigationView)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _navigationView = navigationView ?? throw new ArgumentNullException(nameof(navigationView));
            
            _navigationView.SelectionChanged += NavigationView_SelectionChanged;
            _navigationView.BackRequested += NavigationView_BackRequested;
            _frame.Navigated += Frame_Navigated;
        }

        public bool Navigate<T>(object parameter = null) where T : Page
        {
            if (_frame == null) return false;

            var pageType = typeof(T);
            var result = _frame.Navigate(pageType, parameter);
            if (result)
            {
                UpdateSelectedNavViewItem(pageType.Name);
                NavigationChanged?.Invoke(this, pageType.Name);
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

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (_frame == null || args.SelectedItemContainer is not NavigationViewItem selectedItem) return;

            string tag = (string)selectedItem.Tag;
            Type pageType = GetPageTypeFromTag(tag);
            _frame.Navigate(pageType);
        }

        private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            GoBack();
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (_navigationView == null) return;
            
            _navigationView.IsBackEnabled = _frame?.CanGoBack ?? false;
            UpdateSelectedNavViewItem(e.SourcePageType.Name);
        }

        private void UpdateSelectedNavViewItem(string pageName)
        {
            if (_navigationView == null) return;

            foreach (var item in _navigationView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == pageName)
                {
                    _navigationView.SelectedItem = navItem;
                    break;
                }
            }
        }

        private Type GetPageTypeFromTag(string tag)
        {
            return tag switch
            {
                "SelectProfiles" => typeof(SelectProfilesPage),
                "ActionStudio" => typeof(ActionStudioPage),
                "IconStudio" => typeof(IconStudioPage),
                "ProfileEditor" => typeof(ProfileEditorPage),
                _ => typeof(SelectProfilesPage)
            };
        }
    }
}