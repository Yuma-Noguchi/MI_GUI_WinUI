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
            // Create a new instance while properly handling position updates
            Point newPosition = position ?? Position;
            var updatedElement = (element ?? Element) with { };
            
            // Always update the underlying element's position to match
            updatedElement = updatedElement.WithPosition((int)newPosition.X, (int)newPosition.Y);
            
            return new UnifiedPositionInfo(
                updatedElement,
                newPosition,
                size ?? Size
            );
        }
    }
}
