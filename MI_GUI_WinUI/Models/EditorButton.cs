namespace MI_GUI_WinUI.Models
{
    public record EditorButton
    {
        public string Name { get; init; } = string.Empty;
        public string IconPath { get; init; } = string.Empty;
        public bool IsDefault { get; init; }

        public EditorButton() { }

        public EditorButton(string name, string iconPath, bool isDefault = false)
        {
            Name = name;
            IconPath = iconPath;
            IsDefault = isDefault;
        }
    }
}
