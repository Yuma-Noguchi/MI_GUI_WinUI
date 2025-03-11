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
            Element = element;
            Position = position;
            Button = button;
        }

        public static ElementAddRequest FromExisting(UnifiedGuiElement element, Point position)
        {
            return new ElementAddRequest(element, position);
        }

        public static ElementAddRequest FromButton(EditorButton button, Point position, bool isPose = false)
        {
            var element = isPose ? UnifiedGuiElement.CreatePoseElement() : UnifiedGuiElement.CreateGuiElement();
            element = element.WithSkin(button.FileName);  // Use FileName for relative path
            return new ElementAddRequest(element, position, button);
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
