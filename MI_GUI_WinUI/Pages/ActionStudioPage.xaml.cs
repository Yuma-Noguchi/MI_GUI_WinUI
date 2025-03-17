using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MI_GUI_WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ActionStudioPage : Page
    {
        private readonly ActionStudioViewModel ViewModel;

        public ActionStudioPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<ActionStudioViewModel>();
            DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = XamlRoot;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = null;
            }
        }
    }
}
