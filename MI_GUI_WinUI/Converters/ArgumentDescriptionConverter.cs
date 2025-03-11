using System;
using Microsoft.UI.Xaml.Data;
using MI_GUI_WinUI.ViewModels;

namespace MI_GUI_WinUI.Converters
{
    public class ArgumentDescriptionConverter : IValueConverter
    {
        private static ActionConfigurationDialogViewModel? GetViewModel()
        {
            if (App.Current?.Resources["ActionConfigViewModel"] is ActionConfigurationDialogViewModel vm)
            {
                return vm;
            }
            return null;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string argument)
            {
                var vm = GetViewModel();
                if (vm != null)
                {
                    return vm.GetArgumentDescription(argument);
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
