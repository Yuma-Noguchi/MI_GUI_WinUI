using System;
using Windows.Foundation;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.Models
{
    public class ElementAddRequest
    {
        private const int MOTION_INPUT_WIDTH = 640;
        private const int MOTION_INPUT_HEIGHT = 480;

        public UnifiedGuiElement Element { get; }
        public Point Position { get; }
        public EditorButton? Button { get; }

        public ElementAddRequest(UnifiedGuiElement element, Point position, EditorButton? button = null)
        {
            // Keep original position for head tilt elements
            if (element.File != "head_tilt_joystick.py")
            {
                element = element.WithPosition((int)position.X, (int)position.Y);
            }
            Element = element;
            Position = position;
            Button = button;
        }

        public static ElementAddRequest FromExisting(UnifiedGuiElement element, Point position)
        {
            // Don't update position for head tilt elements during load
            if (element.File == "head_tilt_joystick.py")
            {
                // Keep head tilt centered
                return new ElementAddRequest(
                    element,
                    position  // Use provided position for canvas display
                );
            }

            // Update element position for other elements
            return new ElementAddRequest(
                element.WithPosition((int)position.X, (int)position.Y),
                position
            );
        }

        public static ElementAddRequest FromButton(EditorButton button, Point position, bool isPose = false)
        {
            var element = isPose ? UnifiedGuiElement.CreatePoseElement() : UnifiedGuiElement.CreateGuiElement();
            // Set both position and skin
            element = element
                .WithPosition((int)position.X, (int)position.Y)
                .WithSkin(button.FileName);  // Use FileName for relative path
            return new ElementAddRequest(element, position, button);
        }

        public static ElementAddRequest CreateHeadTiltRequest(Point position, int radius)
        {
            var element = UnifiedGuiElement.CreateHeadTiltElement();
            return new ElementAddRequest(element, position);
        }

        public static ElementAddRequest CreatePoseRequest(Point position, int radius)
        {
            var element = UnifiedGuiElement.CreatePoseElement(
                (int)position.X,
                (int)position.Y,
                radius
            );
            return new ElementAddRequest(element, position);
        }

        public static ElementAddRequest CreateButtonRequest(string imagePath, Point position, int radius)
        {
            // Convert display path to relative path for storage
            string relativePath = Utils.FileNameHelper.ConvertToAssetsRelativePath(imagePath);

            var element = UnifiedGuiElement.CreateGuiElement(
                (int)position.X,
                (int)position.Y,
                radius
            ).WithSkin(relativePath);
            
            return new ElementAddRequest(element, position);
        }
    }
}
