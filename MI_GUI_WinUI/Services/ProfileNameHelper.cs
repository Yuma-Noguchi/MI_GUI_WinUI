using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MI_GUI_WinUI.Services
{
    public static class ProfileNameHelper
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Validates if a profile display name is valid (contains no invalid filename characters)
        /// </summary>
        public static bool IsValidProfileName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            return !displayName.Any(c => InvalidFileNameChars.Contains(c));
        }

        /// <summary>
        /// Converts a display name to a valid filename by replacing spaces with underscores
        /// </summary>
        public static string GetFileNameFromDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty", nameof(displayName));

            if (!IsValidProfileName(displayName))
                throw new ArgumentException("Display name contains invalid characters", nameof(displayName));

            return displayName.Replace(' ', '_');
        }

        /// <summary>
        /// Converts a filename back to display name by replacing underscores with spaces
        /// </summary>
        public static string GetDisplayNameFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            // Remove extension if present
            fileName = Path.GetFileNameWithoutExtension(fileName);
            return fileName.Replace('_', ' ');
        }
    }
}