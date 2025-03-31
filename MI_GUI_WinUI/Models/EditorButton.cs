namespace MI_GUI_WinUI.Models
{
    public record EditorButton
    {
        public string Name { get; init; } = string.Empty;
        public string IconPath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;  // For storing relative path
        public bool IsDefault { get; init; }

        public EditorButton() { }

        public EditorButton(string name, string iconPath, string fileName, bool isDefault = false)
        {
            Name = name;
            IconPath = iconPath;
            FileName = fileName;
            IsDefault = isDefault;
        }
    }
}
