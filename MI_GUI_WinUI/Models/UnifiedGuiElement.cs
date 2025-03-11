using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    public record UnifiedGuiElement
    {
        [JsonProperty("file")]
        public string File { get; init; } = string.Empty;

        [JsonProperty("pos")]
        public List<int> Position { get; init; } = new List<int>();

        [JsonProperty("radius")]
        public int Radius { get; init; }

        [JsonProperty("skin")]
        public string Skin { get; init; } = string.Empty;

        [JsonProperty("left_skin")]
        public string? LeftSkin { get; init; }

        [JsonProperty("right_skin")]
        public string? RightSkin { get; init; }

        [JsonProperty("sensitivity")]
        public double? Sensitivity { get; init; }

        [JsonProperty("deadzone")]
        public int? Deadzone { get; init; }

        [JsonProperty("linear")]
        public bool? Linear { get; init; }

        [JsonProperty("landmarks")]
        private List<string>? _landmarks;

        // Public property for landmarks that's never null
        [JsonIgnore]
        public List<string> Landmarks 
        {
            get => _landmarks ?? new List<string>();
            init => _landmarks = value;
        }

        [JsonProperty("action")]
        public Models.ActionConfig Action { get; init; } = new();

        [JsonIgnore]
        public bool IsPose => _landmarks?.Count > 0 || LeftSkin != null || RightSkin != null;

        public static UnifiedGuiElement CreateGuiElement(int x = 0, int y = 0, int radius = 30)
        {
            return new UnifiedGuiElement
            {
                File = "button",
                Position = new List<int> { x, y },
                Radius = radius,
                Action = new Models.ActionConfig
                {
                    ClassName = "ds4_gamepad",
                    MethodName = "",
                    Arguments = new List<string>()
                }
            };
        }

        public static UnifiedGuiElement CreatePoseElement(int x = 0, int y = 0, int radius = 30)
        {
            return new UnifiedGuiElement
            {
                File = "hit_trigger.py",
                Position = new List<int> { x, y },
                Radius = radius,
                Action = new Models.ActionConfig
                {
                    ClassName = "ds4_gamepad",
                    MethodName = "",
                    Arguments = new List<string>()
                },
                _landmarks = new List<string> { "RIGHT_WRIST" },
                Sensitivity = 1.0,
                Deadzone = 10,
                Linear = true
            };
        }

        public UnifiedGuiElement WithPosition(int x, int y) => this with
        {
            Position = new List<int> { x, y }
        };

        public UnifiedGuiElement WithRadius(int radius) => this with
        {
            Radius = radius
        };

        public UnifiedGuiElement WithSkin(string skin) => this with
        {
            Skin = skin
        };

        public UnifiedGuiElement WithAction(Models.ActionConfig action) => this with
        {
            Action = action
        };

        public UnifiedGuiElement WithLandmarks(List<string> landmarks) => this with
        {
            _landmarks = landmarks.Count > 0 ? landmarks : null
        };

        public UnifiedGuiElement WithPoseSettings(double sensitivity, int deadzone, bool linear) => this with
        {
            Sensitivity = sensitivity,
            Deadzone = deadzone,
            Linear = linear
        };

        public GuiElement ToGuiElement() => new GuiElement
        {
            File = File,
            Position = Position,
            Radius = Radius,
            Skin = Skin,
            Action = Action
        };

        public PoseGuiElement ToPoseElement() => new PoseGuiElement
        {
            File = File,
            Position = Position,
            Radius = Radius,
            Skin = Skin,
            LeftSkin = LeftSkin ?? string.Empty,
            RightSkin = RightSkin ?? string.Empty,
            Sensitivity = Sensitivity ?? 1.0,
            Deadzone = Deadzone ?? 10,
            Linear = Linear ?? true,
            Landmark = Landmarks.FirstOrDefault() ?? string.Empty,
            Action = Action
        };

        public static UnifiedGuiElement FromGuiElement(GuiElement element) => new()
        {
            File = element.File,
            Position = element.Position,
            Radius = element.Radius,
            Skin = element.Skin,
            Action = element.Action
        };

        public static UnifiedGuiElement FromPoseElement(PoseGuiElement element) => new()
        {
            File = element.File,
            Position = element.Position,
            Radius = element.Radius,
            Skin = element.Skin,
            LeftSkin = element.LeftSkin,
            RightSkin = element.RightSkin,
            Sensitivity = element.Sensitivity,
            Deadzone = element.Deadzone,
            Linear = element.Linear,
            _landmarks = string.IsNullOrEmpty(element.Landmark) ? null : new List<string> { element.Landmark },
            Action = element.Action
        };
    }
}
