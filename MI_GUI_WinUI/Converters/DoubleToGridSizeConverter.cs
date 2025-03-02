using Microsoft.UI.Xaml.Data;
using System;

namespace MI_GUI_WinUI.Converters
{
    public class DoubleToGridSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double radius)
            {
                return radius * 2; // Make the grid twice the radius to account for full button size
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
