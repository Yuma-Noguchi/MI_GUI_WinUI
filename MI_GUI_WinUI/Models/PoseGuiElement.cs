using System.Collections.Generic;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    public class PoseGuiElement
    {
        [JsonProperty("file")]
        public string File { get; set; } = string.Empty;

        [JsonProperty("pos")]
        public List<int>? Position { get; set; }

        [JsonProperty("radius")]
        public int Radius { get; set; }

        [JsonProperty("skin")]
        public string Skin { get; set; } = string.Empty;

        [JsonProperty("left_skin")]
        public string LeftSkin { get; set; } = string.Empty;

        [JsonProperty("right_skin")]
        public string RightSkin { get; set; } = string.Empty;

        [JsonProperty("sensitivity")]
        public double Sensitivity { get; set; } = 1.0;

        [JsonProperty("deadzone")]
        public double Deadzone { get; set; }

        [JsonProperty("linear")]
        public bool Linear { get; set; }

        [JsonProperty("flag")]
        public int Flag { get; set; }

        [JsonProperty("landmark")]
        public string? Landmark { get; set; }

        [JsonProperty("landmarks")]
        public List<string> Landmarks { get; set; } = new();

        [JsonProperty("action")]
        public ActionConfig Action { get; set; } = new();
    }
}
