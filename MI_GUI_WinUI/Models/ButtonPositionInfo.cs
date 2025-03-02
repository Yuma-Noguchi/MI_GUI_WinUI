using Windows.Foundation;
using System;

namespace MI_GUI_WinUI.Models
{
    public class ButtonPositionInfo
    {
        private EditorButton _button = null!;
        private Point _position;
        private Size _size;

        public required EditorButton Button
        {
            get => _button;
            init => _button = value ?? throw new ArgumentNullException(nameof(value));
        }

        public required Point Position
        {
            get => _position;
            set => _position = value;
        }

        public Point? SnapPosition { get; set; }

        public Size Size
        {
            get => _size;
            set => _size = value;
        }

        public ButtonPositionInfo()
        {
            _size = new Size(60, 60);
        }
    }
}
