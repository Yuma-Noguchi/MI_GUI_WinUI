using System.Drawing;

namespace MI_GUI_WinUI.Models
{
    public class IconGenerationSettings
    {
        public string Prompt { get; set; } = string.Empty;
        public Size ImageSize { get; set; } = new Size(60, 60); // Fixed size for initial implementation
    }
}
