using Microsoft.UI.Xaml.Data;
using System;

namespace MI_GUI_WinUI.Converters
{
    public class ButtonDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string buttonName)
            {
                string displayName = buttonName;
                if (buttonName.StartsWith("DPad_"))
                {
                    displayName = buttonName.Substring(5); // Remove "DPad_" prefix
                }
                // Capitalize the name
                if (!string.IsNullOrEmpty(displayName))
                {
                    return char.ToUpper(displayName[0]) + displayName.Substring(1);
                }
                return displayName;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value; // No need to convert back as we keep original values in the backend
        }
    }
}
