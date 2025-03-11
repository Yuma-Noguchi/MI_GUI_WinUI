using System;

namespace MI_GUI_WinUI.Models
{
    public record MethodDescription
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }

        public MethodDescription(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;
    }
}
