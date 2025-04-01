using System;
using Windows.Foundation;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.Models
{
    public class ElementAddRequest
    {
        public UnifiedGuiElement Element { get; }
        public Point Position { get; }
        public EditorButton? Button { get; }

        public ElementAddRequest(UnifiedGuiElement element, Point position, EditorButton? button = null)
        {
            // Update element position during creation
            element = element.WithPosition((int)position.X, (int)position.Y);
            Element = element;
            Position = position;
            Button = button;
        }

        public static ElementAddRequest FromExisting(UnifiedGuiElement element, Point position)
        {
            // Update element position for existing elements
            return new ElementAddRequest(element.WithPosition((int)position.X, (int)position.Y), position);
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
            var element = UnifiedGuiElement.CreateHeadTiltElement(
                (int)position.X,
                (int)position.Y,
                radius
            );
            return new ElementAddRequest(element, position);
        }

        public static ElementAddRequest CreatePoseRequest(Point position, int radius)
        {
            var element = UnifiedGuiElement.CreatePoseElement(
                (int)position.X,  // Pass actual position
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
                (int)position.X,  // Pass actual position
                (int)position.Y,
                radius
            ).WithSkin(relativePath);
            
            return new ElementAddRequest(element, position);
        }
    }
}
