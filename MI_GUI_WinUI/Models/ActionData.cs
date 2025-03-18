using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Converters;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    [JsonConverter(typeof(ActionJsonConverter))]
    public partial class ActionData : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private ObservableCollection<SequenceItem> _sequence = new();

        [JsonProperty("class")]
        public string Class { get; set; } = "ds4_gamepad";

        [JsonProperty("method")]
        public string Method { get; set; } = "chain";

        // Helper methods for creating sequence items
        public static SequenceItem CreateButtonPress(string buttonName)
        {
            return new SequenceItem
            {
                Type = "press",
                Value = buttonName.ToLower()
            };
        }

        public static SequenceItem CreateSleep(double duration)
        {
            return new SequenceItem
            {
                Type = "sleep",
                Value = duration.ToString()
            };
        }

        public override string ToString()
        {
            return $"{Name} ({Sequence.Count} items)";
        }

        public override bool Equals(object obj)
        {
            if (obj is ActionData other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
