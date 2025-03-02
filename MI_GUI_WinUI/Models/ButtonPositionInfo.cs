using Windows.Foundation;

namespace MI_GUI_WinUI.Models
{
    public class ButtonPositionInfo
    {
        public required EditorButton Button { get; init; }
        public required Point Position { get; init; }

        public static ButtonPositionInfo Create(EditorButton button, Point position)
        {
            return new ButtonPositionInfo
            {
                Button = button,
                Position = position
            };
        }
    }
}
