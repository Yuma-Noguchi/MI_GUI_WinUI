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
                    _sequence.CollectionChanged += OnSequenceChanged;
                }
                return _sequence;
            }
            set
            {
                if (_sequence != null)
                {
                    _sequence.CollectionChanged -= OnSequenceChanged;
                }
                _sequence = value ?? new ObservableCollection<SequenceItem>();
                _sequence.CollectionChanged += OnSequenceChanged;
                UpdateArgsFromSequence();
            }
        }

        private void OnSequenceChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateArgsFromSequence();
        }

        private void UpdateArgsFromSequence()
        {
            Args = _sequence?.Select(item => item.ToDictionary()).ToList() ?? new List<Dictionary<string, object>>();
        }

        public ActionData()
        {
            Id = Guid.NewGuid().ToString();
            Args = new List<Dictionary<string, object>>();
        }

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

        // For comparing actions in UI
        public override bool Equals(object obj)
        {
            if (obj is ActionData other)
            {
                return other.Id == Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
