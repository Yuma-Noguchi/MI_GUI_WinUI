using System;

namespace MI_GUI_WinUI.Services
{
    public class ActionNameExistsException : Exception
    {
        public ActionNameExistsException(string name)
            : base($"An action with the name '{name}' already exists")
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class ActionNotFoundException : Exception
    {
        public ActionNotFoundException(string name)
            : base($"Action with name '{name}' not found")
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class InvalidActionException : Exception
    {
        public InvalidActionException(string message)
            : base(message)
        {
        }
    }
}