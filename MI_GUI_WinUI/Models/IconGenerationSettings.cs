using System.Threading;

namespace MI_GUI_WinUI.Models
{
    public class IconGenerationSettings
    {
        public string Prompt { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int NumInferenceSteps { get; set; }
        public double GuidanceScale { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public IconGenerationSettings()
        {
            // Default values
            Width = 512;
            Height = 512;
            NumInferenceSteps = 20;
            GuidanceScale = 7.5f;
            CancellationToken = CancellationToken.None;
        }
    }
}
