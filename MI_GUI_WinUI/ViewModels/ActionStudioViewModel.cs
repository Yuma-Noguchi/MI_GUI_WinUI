using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ActionStudioViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Action Studio";
    }
}