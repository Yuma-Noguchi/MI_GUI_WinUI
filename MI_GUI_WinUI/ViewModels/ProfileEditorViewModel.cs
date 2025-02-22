using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class ProfileEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Profile Editor";
    }
}