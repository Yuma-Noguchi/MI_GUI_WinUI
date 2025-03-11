using System;
using System.IO;
using System.Linq;

namespace MI_GUI_WinUI.Utils
{
    public static class FileNameHelper
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return !fileName.Any(c => InvalidFileNameChars.Contains(c));
        }

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            foreach (char c in InvalidFileNameChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName.Trim();
        }

        public static string ConvertToAssetsRelativePath(string fullPath)
        {
            // Extract everything after "assets/"
            int index = fullPath.IndexOf("assets/");
            if (index >= 0)
            {
                return fullPath.Substring(index + "assets/".Length);
            }
            return fullPath;
        }

        public static string GetFullAssetPath(string relativePath)
        {
            return $"ms-appx:///MotionInput/data/assets/{relativePath}";
        }
    }
}
