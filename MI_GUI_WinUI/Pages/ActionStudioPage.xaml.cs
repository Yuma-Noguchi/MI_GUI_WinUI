using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.ViewModels;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ActionStudioPage : Page
    {
        public ActionStudioViewModel ViewModel { get; }

        public ActionStudioPage()
        {
            ViewModel = new ActionStudioViewModel();
            this.InitializeComponent();
        }
    }
}
