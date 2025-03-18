using Microsoft.UI.Xaml.Data;
using System;

namespace MI_GUI_WinUI.Converters
{
    public class BoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && b ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is int i && i == 1;
        }
    }
}
