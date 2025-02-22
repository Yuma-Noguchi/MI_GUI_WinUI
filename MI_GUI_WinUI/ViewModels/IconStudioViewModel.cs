using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Icon Studio";
    }
}