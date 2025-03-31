using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    public class SequenceItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonIgnore]
        public bool IsPress => Type == "press";

        [JsonIgnore]
        public bool IsSleep => Type == "sleep";

        public double SleepDuration => IsSleep && double.TryParse(Value, out double result) ? result : 0;
        public string ButtonName => IsPress ? Value : string.Empty;

        public static SequenceItem CreateButtonPress(string button)
        {
            return new SequenceItem 
            { 
                Type = "press",
                Value = button.ToLower()
            };
        }

        public static SequenceItem CreateSleep(double seconds)
        {
            return new SequenceItem
            {
                Type = "sleep",
                Value = seconds.ToString()
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            object valueArray;
            if (IsSleep)
            {
                valueArray = new[] { Convert.ToDouble(Value) };
            }
            else
            {
                valueArray = new[] { Value };
            }

            return new Dictionary<string, object>
            {
                { Type, valueArray }
            };
        }

        public static SequenceItem FromDictionary(Dictionary<string, object> dict)
        {
            var pair = dict.FirstOrDefault();
            if (pair.Key == null) return CreateSleep(1.0);

            var type = pair.Key;
            var values = pair.Value as object[];
            var value = values?.FirstOrDefault()?.ToString() ?? "1.0";

            return new SequenceItem
            {
                Type = type,
                Value = value
            };
        }

        public override string ToString()
        {
            return IsSleep ? $"Sleep: {Value}s" : $"Press: {Value}";
        }
    }
}
