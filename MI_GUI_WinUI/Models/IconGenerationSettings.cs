using System.Drawing;

namespace MI_GUI_WinUI.Models
{
    public class IconGenerationSettings
    {
        public string Prompt { get; set; } = string.Empty;
        public Size ImageSize { get; set; } = new(60, 60); // Fixed size for icons
        public int NumInferenceSteps { get; set; } = 20;
        public float GuidanceScale { get; set; } = 7.5f;
        public long Seed { get; set; } = -1;  // -1 means random seed
    }
}
