using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    public class ActionData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; } = "ds4_gamepad";

        [JsonProperty("method")]
        public string Method { get; set; } = "chain";

        [JsonProperty("args")]
        public List<Dictionary<string, object>> Args { get; set; }

        [JsonIgnore]
        private ObservableCollection<SequenceItem> _sequence;

        [JsonIgnore]
        public ObservableCollection<SequenceItem> Sequence
        {
            get
            {
                if (_sequence == null)
                {
                    _sequence = new ObservableCollection<SequenceItem>(
                        Args?.Select(dict => SequenceItem.FromDictionary(dict)) ?? 
                        Enumerable.Empty<SequenceItem>()
                    );
                    _sequence.CollectionChanged += (s, e) =>
                    {
                        // Update Args when Sequence changes
                        Args = _sequence.Select(item => item.ToDictionary()).ToList();
                    };
                }
                return _sequence;
            }
            set
            {
                _sequence = value;
                Args = _sequence?.Select(item => item.ToDictionary()).ToList() ?? 
                      new List<Dictionary<string, object>>();
            }
        }

        public ActionData()
        {
            Id = Guid.NewGuid().ToString();
            Args = new List<Dictionary<string, object>>();
        }

        // Helper method to create an action with button press
        public static ActionData CreateButtonPress(string button)
        {
            var action = new ActionData();
            action.Sequence.Add(SequenceItem.CreateButtonPress(button));
            return action;
        }

        // Helper method to create an action with sleep
        public static ActionData CreateSleep(double seconds)
        {
            var action = new ActionData();
            action.Sequence.Add(SequenceItem.CreateSleep(seconds));
            return action;
        }

        // Helper method to clone an action
        public ActionData Clone()
        {
            var clone = new ActionData
            {
                Id = Guid.NewGuid().ToString(), // Generate new ID for clone
                Name = this.Name + " (Copy)",
                Class = this.Class,
                Method = this.Method
            };

            foreach (var item in this.Sequence)
            {
                clone.Sequence.Add(new SequenceItem
                {
                    Type = item.Type,
                    Value = item.Value
                });
            }

            return clone;
        }

        // Helper method for migration
        public static ActionData FromLegacyFormat(Dictionary<string, object> data)
        {
            var action = new ActionData();

            if (data.TryGetValue("id", out var id))
                action.Id = id.ToString();

            if (data.TryGetValue("name", out var name))
                action.Name = name.ToString();

            if (data.TryGetValue("sequence", out var sequence) && sequence is Dictionary<string, object> seq)
            {
                if (seq.TryGetValue("action", out var actionObj) && actionObj is Dictionary<string, object> actionDict)
                {
                    if (actionDict.TryGetValue("class", out var classValue))
                        action.Class = classValue.ToString();

                    if (actionDict.TryGetValue("method", out var methodValue))
                        action.Method = methodValue.ToString();

                    if (actionDict.TryGetValue("args", out var args) && args is List<object> argsList)
                    {
                        foreach (var arg in argsList)
                        {
                            if (arg is Dictionary<string, object> dict)
                            {
                                action.Args.Add(dict);
                            }
                        }
                    }
                }
            }

            return action;
        }
    }
}
