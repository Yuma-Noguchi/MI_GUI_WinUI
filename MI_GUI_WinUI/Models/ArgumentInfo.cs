using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace MI_GUI_WinUI.Models
{
    public partial class ArgumentInfo : ObservableObject
    {
        [ObservableProperty]
        private string value = string.Empty;

        public string Description { get; init; }
        public bool IsButton { get; init; }
        public List<string> ButtonOptions { get; init; } = new();

        public ArgumentInfo(string description, string value = "", bool isButton = false)
        {
            Description = description;
            Value = value;
            IsButton = isButton;
            if (isButton)
            {
                ButtonOptions = new List<string> 
                {
                    "A", "B", "X", "Y",
                    "LB", "RB", "LT", "RT",
                    "Start", "Back", "LS", "RS",
                    "DPad_Up", "DPad_Down", "DPad_Left", "DPad_Right"
                };
            }
        }
    }
}
