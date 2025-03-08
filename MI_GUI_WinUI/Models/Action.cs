using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.Models
{
    public enum ActionType
    {
        Basic,
        Sequence,
        Macro,
        Custom
    }

    public partial class Action : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private List<InputSequence> _sequences;

        [ObservableProperty]
        private ActionType _type;

        [ObservableProperty]
        private bool _isEnabled;

        public Action()
        {
            _id = Guid.NewGuid().ToString();
            _sequences = new List<InputSequence>();
            _type = ActionType.Basic;
            _isEnabled = true;
        }
    }
}
