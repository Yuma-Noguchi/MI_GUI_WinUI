using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Foundation;

namespace MI_GUI_WinUI.Models
{
    public partial class EditorButton : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _file = string.Empty;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _iconPath = string.Empty;

        [ObservableProperty]
        private string _skin = string.Empty;

        [ObservableProperty]
        private string _triggeredSkin = string.Empty;

        [ObservableProperty]
        private int _radius;

        [ObservableProperty]
        private Point _position;

        [ObservableProperty]
        private ActionConfig _action = new()
        {
            ClassName = string.Empty,
            MethodName = string.Empty,
            Arguments = new System.Collections.Generic.List<string>()
        };

        public GuiElement ToGuiElement()
        {
            return new GuiElement
            {
                File = File,
                Position = new System.Collections.Generic.List<int> { (int)Position.X, (int)Position.Y },
                Radius = Radius,
                Skin = Skin,
                TriggeredSkin = TriggeredSkin,
                Action = Action
            };
        }

        public static EditorButton FromGuiElement(GuiElement element)
        {
            return new EditorButton
            {
                File = element.File,
                Name = element.File, // Use File as Name for display purposes
                Position = new Point(element.Position[0], element.Position[1]),
                Radius = element.Radius,
                Skin = element.Skin,
                TriggeredSkin = element.TriggeredSkin,
                Action = element.Action,
                IconPath = element.File // Set IconPath to File for image display
            };
        }

        public EditorButton Clone()
        {
            return new EditorButton
            {
                Name = Name,
                File = File,
                Category = Category,
                IconPath = IconPath,
                Skin = Skin,
                TriggeredSkin = TriggeredSkin,
                Radius = Radius,
                Position = Position,
                Action = new ActionConfig
                {
                    ClassName = Action.ClassName,
                    MethodName = Action.MethodName,
                    Arguments = new System.Collections.Generic.List<string>(Action.Arguments)
                }
            };
        }
    }
}
