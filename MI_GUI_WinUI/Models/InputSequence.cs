using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.Models
{
    public enum InputType
    {
        Press,
        Hold,
        Release
    }

    public partial class Input : ObservableObject
    {
        [ObservableProperty]
        private InputType _type;

        [ObservableProperty]
        private string _button;

        [ObservableProperty]
        private int _duration;

        [ObservableProperty]
        private int _startTime;

        public Input()
        {
            _type = InputType.Press;
            _duration = 0;
            _startTime = 0;
        }
    }

    public partial class InputSequence : ObservableObject
    {
        [ObservableProperty]
        private List<Input> _inputs;

        [ObservableProperty]
        private int _duration;

        [ObservableProperty]
        private int _delay;

        public InputSequence()
        {
            _inputs = new List<Input>();
            _duration = 0;
            _delay = 0;
        }
    }
}
