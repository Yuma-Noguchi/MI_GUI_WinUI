using Microsoft.UI.Xaml.Data;
using System;

namespace MI_GUI_WinUI.Converters
{
    public class NumberToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return "";

            if (value is double doubleValue)
                return doubleValue.ToString("0.#"); // Format with at most 1 decimal place

            if (value is float floatValue)
                return floatValue.ToString("0.#");

            if (value is int intValue)
                return intValue.ToString();

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
