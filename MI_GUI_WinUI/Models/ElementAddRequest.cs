using Windows.Foundation;
using System.Collections.Generic;

namespace MI_GUI_WinUI.Models
{
    public enum ElementType
    {
        Button,
        Pose
    }

    public class ElementAddRequest
    {
        public UnifiedGuiElement Element { get; }
        public Point Position { get; }
        public ElementType ElementType { get; }

        public ElementAddRequest(UnifiedGuiElement element, Point position, ElementType elementType)
        {
            Element = element;
            Position = position;
            ElementType = elementType;
        }

        public static ElementAddRequest CreateButtonRequest(string skin, Point position, int radius = 30)
        {
            var element = UnifiedGuiElement.CreateGuiElement(
                x: (int)position.X + radius,
                y: (int)position.Y + radius,
                radius: radius
            ).WithSkin(skin);

            return new ElementAddRequest(element, position, ElementType.Button);
        }

        public static ElementAddRequest CreatePoseRequest(Point position, int radius = 30)
        {
            var element = UnifiedGuiElement.CreatePoseElement(
                x: (int)position.X + radius,
                y: (int)position.Y + radius,
                radius: radius
            );

            return new ElementAddRequest(element, position, ElementType.Pose);
        }

        public static ElementAddRequest FromExisting(UnifiedGuiElement element, Point position)
        {
            return new ElementAddRequest(
                element,
                position,
                element.IsPose ? ElementType.Pose : ElementType.Button
            );
        }
    }
}
