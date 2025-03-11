using Windows.Foundation;

namespace MI_GUI_WinUI.Models
{
    public record struct UnifiedPositionInfo
    {
        public UnifiedGuiElement Element { get; init; }
        public Point Position { get; init; }
        public Size Size { get; init; }

        public UnifiedPositionInfo(UnifiedGuiElement element, Point position, Size size) : this()
        {
            Element = element;
            Position = position;
            Size = size;
        }

        public UnifiedPositionInfo With(UnifiedGuiElement? element = null, Point? position = null, Size? size = null) => new()
        {
            Element = element ?? Element,
            Position = position ?? Position,
            Size = size ?? Size
        };
    }
}
