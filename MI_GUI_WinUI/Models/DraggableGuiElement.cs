using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Foundation;

namespace MI_GUI_WinUI.Models
{
    public partial class DraggableGuiElement : ObservableObject
    {
        private GuiElement _element;

        [ObservableProperty]
        private double _x;

        [ObservableProperty]
        private double _y;

        public DraggableGuiElement(GuiElement element)
        {
            _element = element;
            
            // Initialize with default values if not set
            _element.File = string.IsNullOrEmpty(_element.File) ? "default_button.png" : _element.File;
            _element.Radius = _element.Radius == 0 ? 50 : _element.Radius;
            _element.Skin = string.IsNullOrEmpty(_element.Skin) ? "default_button.png" : _element.Skin;
            _element.TriggeredSkin = string.IsNullOrEmpty(_element.TriggeredSkin) ? "default_button_pressed.png" : _element.TriggeredSkin;
            
            // Initialize position
            _element.Position ??= new List<int>();
            while (_element.Position.Count < 2)
                _element.Position.Add(0);
            
            X = _element.Position[0];
            Y = _element.Position[1];

            // Initialize action with defaults if needed
            if (string.IsNullOrEmpty(_element.Action.ClassName))
            {
                _element.Action = new ActionConfig
                {
                    ClassName = "DefaultAction",
                    MethodName = "Click",
                    Arguments = new List<string>()
                };
            }
        }

        public string File
        {
            get => _element.File;
            set
            {
                _element.File = value;
                OnPropertyChanged();
            }
        }

        public double Radius
        {
            get => _element.Radius;
            set
            {
                _element.Radius = (int)value;
                OnPropertyChanged();
            }
        }

        public string Skin
        {
            get => _element.Skin;
            set
            {
                _element.Skin = value;
                OnPropertyChanged();
            }
        }

        public string TriggeredSkin
        {
            get => _element.TriggeredSkin;
            set
            {
                _element.TriggeredSkin = value;
                OnPropertyChanged();
            }
        }

        public ActionConfig Action
        {
            get => _element.Action;
            set
            {
                _element.Action = value;
                OnPropertyChanged();
            }
        }

        // Store position in the underlying GuiElement
        partial void OnXChanged(double value)
        {
            _element.Position ??= new List<int>();
            while (_element.Position.Count < 2)
                _element.Position.Add(0);
            _element.Position[0] = (int)value;
        }

        partial void OnYChanged(double value)
        {
            _element.Position ??= new List<int>();
            while (_element.Position.Count < 2)
                _element.Position.Add(0);
            _element.Position[1] = (int)value;
        }

        public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(File);

        public DraggableGuiElement Clone()
        {
            var clonedElement = new GuiElement
            {
                File = this.File,
                Position = new List<int> { (int)this.X, (int)this.Y },
                Radius = (int)this.Radius,
                Skin = this.Skin,
                TriggeredSkin = this.TriggeredSkin,
                Action = this.Action
            };

            return new DraggableGuiElement(clonedElement);
        }

        public GuiElement ToGuiElement()
        {
            // Ensure position is updated
            OnXChanged(X);
            OnYChanged(Y);
            return _element;
        }
    }
}
