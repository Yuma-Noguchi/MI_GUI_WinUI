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

        public UnifiedPositionInfo With(UnifiedGuiElement? element = null, Point? position = null, Size? size = null)
        {
            // Create a new instance while properly handling the Element reference
            var updatedElement = element ?? Element with { };
            
            return new UnifiedPositionInfo(
                updatedElement,
                position ?? Position,
                size ?? Size
            );
        }
    }
}
