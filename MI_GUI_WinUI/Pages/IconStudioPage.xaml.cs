using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MI_GUI_WinUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class IconStudioPage : Page
    {
        private IconStudioViewModel? _viewModel;

        public IconStudioPage()
        {
            this.InitializeComponent();
            if (_viewModel == null)
            {
                _viewModel = Ioc.Default.GetRequiredService<IconStudioViewModel>();
                SetViewModel(_viewModel);
            }
        }

        private void SetViewModel(IconStudioViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (e.Parameter is IconStudioViewModel vm)
                {
                    SetViewModel(vm);
                }

                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorMessage = $"Error initializing page: {ex.Message}";
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _viewModel?.Cleanup();
            _viewModel = null;
        }
    }
}