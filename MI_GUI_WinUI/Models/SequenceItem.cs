using System;
using CommunityToolkit.Mvvm.ComponentModel;
using MI_GUI_WinUI.Converters;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    [JsonConverter(typeof(SequenceItemJsonConverter))]
    public partial class SequenceItem : ObservableObject
    {
        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        // Helper properties for XAML binding
        public bool IsPress => Type == "press";
        public bool IsSleep => Type == "sleep";

        public double SleepDuration => IsSleep && double.TryParse(Value, out double result) ? result : 0;
        public string ButtonName => IsPress ? Value : string.Empty;

        public override string ToString()
        {
            return IsPress ? $"Press {ButtonName}" : $"Sleep {SleepDuration:F1}s";
        }

        public override bool Equals(object obj)
        {
            if (obj is SequenceItem other)
            {
                return Type == other.Type && Value == other.Value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }
    }
}
