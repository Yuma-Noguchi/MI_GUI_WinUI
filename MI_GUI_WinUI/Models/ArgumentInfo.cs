using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.Models
{
    public partial class ArgumentInfo : ObservableObject
    {
        [ObservableProperty]
        private string value = string.Empty;

        public string Description { get; init; }

        public ArgumentInfo(string description, string value = "")
        {
            Description = description;
            Value = value;
        }
    }
}
