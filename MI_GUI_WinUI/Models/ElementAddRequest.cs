using Windows.Foundation;
using System.Collections.Generic;

namespace MI_GUI_WinUI.Models
{
    public class ElementAddRequest
    {
        public UnifiedGuiElement Element { get; }
        public Point Position { get; }
        public int Radius { get; }

        private ElementAddRequest(UnifiedGuiElement element, Point dropPosition, int radius)
        {
            Radius = radius;
            // Store the top-left position for the visual element
            Position = new Point(dropPosition.X, dropPosition.Y);

            // Update the element's center position
            var centerX = (int)(dropPosition.X + radius);
            var centerY = (int)(dropPosition.Y + radius);
            
            Element = element with
            {
                Position = new List<int> { centerX, centerY },
                Radius = radius
            };
        }

        public static ElementAddRequest CreateButtonRequest(string imagePath, Point position, int radius)
        {
            var element = UnifiedGuiElement.CreateGuiElement() with
            {
                Skin = imagePath,
                Action = new ActionConfig
                {
                    ClassName = "ds4_gamepad",
                    MethodName = "button_down",
                    Arguments = new List<object> { "A" }
                }
            };
            
            return new ElementAddRequest(element, position, radius);
        }

        public static ElementAddRequest CreatePoseRequest(Point position, int radius)
        {
            var element = UnifiedGuiElement.CreatePoseElement() with
            {
                Skin = "ms-appx:///MotionInput/data/assets/racing/forward.png",
                Landmarks = new List<string> { "RIGHT_WRIST" },
                Action = new ActionConfig
                {
                    ClassName = "ds4_gamepad",
                    MethodName = "right_trigger",
                    Arguments = new List<object> { "0.75" }
                }
            };

            return new ElementAddRequest(element, position, radius);
        }

        public static ElementAddRequest FromExisting(UnifiedGuiElement element, Point position)
        {
            // Calculate the top-left position from center
            var topLeftX = position.X - element.Radius;
            var topLeftY = position.Y - element.Radius;
            return new ElementAddRequest(element, new Point(topLeftX, topLeftY), element.Radius);
        }
    }
}
