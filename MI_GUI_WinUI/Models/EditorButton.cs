using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MI_GUI_WinUI.Models
{
    public class EditorButton
    {
        public string Name { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public Size Size { get; set; } = new Size(60, 60);
        public string Category { get; set; } = string.Empty;
        public string? Action { get; set; }
        public bool IsDefault { get; set; }
        
        public EditorButton Clone()
        {
            return new EditorButton
            {
                Name = this.Name,
                IconPath = this.IconPath,
                Size = this.Size,
                Category = this.Category,
                Action = this.Action,
                IsDefault = this.IsDefault
            };
        }
    }
}
