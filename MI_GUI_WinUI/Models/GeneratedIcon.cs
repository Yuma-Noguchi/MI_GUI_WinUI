using System;

namespace MI_GUI_WinUI.Models
{
    public class GeneratedIcon
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
    }
}
