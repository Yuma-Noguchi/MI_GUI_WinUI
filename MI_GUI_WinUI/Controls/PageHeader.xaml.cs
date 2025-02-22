using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Pages;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class PageHeader : UserControl
    {
        private readonly INavigationService _navigationService;

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public PageHeader()
        {
            this.InitializeComponent();
            _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate<HomePage>();
        }
    }
}