using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MI_GUI_WinUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class IconStudioPage : Page
    {
        private readonly ILogger<IconStudioPage>? _logger;

        private readonly IconStudioViewModel ViewModel;

        public IconStudioPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetService<IconStudioViewModel>();
            DataContext = ViewModel;
        }
    }
}
