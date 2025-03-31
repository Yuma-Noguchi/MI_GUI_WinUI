using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Models
{
    public partial class Action : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private ObservableCollection<SequenceItem> _sequence = new();

        // Used for JSON serialization
        [JsonIgnore]
        public ActionData AsActionData
        {
            get
            {
                var actionData = new ActionData
                {
                    Name = Name,
                    Sequence = new ObservableCollection<SequenceItem>()
                };

                foreach (var item in Sequence)
                {
                    actionData.Sequence.Add(item);
                }

                return actionData;
            }
        }
    }
}
