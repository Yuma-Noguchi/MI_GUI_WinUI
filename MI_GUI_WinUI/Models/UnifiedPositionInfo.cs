using Windows.Foundation;
using System.Runtime.CompilerServices;

namespace MI_GUI_WinUI.Models
{
    public class UnifiedPositionInfo
    {
        public UnifiedGuiElement Element { get; }
        public Point Position { get; set; }
        public Size Size { get; set; }

        public UnifiedPositionInfo(UnifiedGuiElement element, Point position, Size size)
        {
            Element = element;
            Position = position;
            Size = size;
        }

        public UnifiedPositionInfo Clone()
        {
            return new UnifiedPositionInfo(
                Element with { }, // Use record's with expression to clone the element
                new Point(Position.X, Position.Y),
                new Size(Size.Width, Size.Height)
            );
        }

        public UnifiedPositionInfo WithPosition(Point newPosition)
        {
            return new UnifiedPositionInfo(
                Element with 
                { 
                    Position = new System.Collections.Generic.List<int> 
                    { 
                        (int)(newPosition.X + Size.Width/2), 
                        (int)(newPosition.Y + Size.Height/2) 
                    } 
                },
                newPosition,
                Size
            );
        }

        public UnifiedPositionInfo WithSize(Size newSize)
        {
            return new UnifiedPositionInfo(
                Element with { Radius = (int)(newSize.Width/2) },
                Position,
                newSize
            );
        }
    }
}
