using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using MI_GUI_WinUI.Models;
using System;

namespace MI_GUI_WinUI.Converters
{
    public class ProgressStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is GenerationProgressState state)
            {
                var resourceKey = state switch
                {
                    GenerationProgressState.Loading => "SystemFillColorCautionBrush",
                    GenerationProgressState.Tokenizing => "SystemFillColorAttentionBrush",
                    GenerationProgressState.Encoding => "SystemFillColorSuccessBrush",
                    GenerationProgressState.InitializingLatents => "AccentFillColorDefaultBrush",
                    GenerationProgressState.Diffusing => "AccentFillColorSecondaryBrush",
                    GenerationProgressState.Decoding => "AccentFillColorTertiaryBrush",
                    GenerationProgressState.Finalizing => "SystemAccentLightColorBrush1",
                    _ => "AccentFillColorDefaultBrush"
                };

                var sourceBrush = (Brush)Application.Current.Resources[resourceKey];
                var brush = parameter as string == "shadow" 
                    ? new SolidColorBrush(((SolidColorBrush)sourceBrush).Color) { Opacity = 0.4 }
                    : sourceBrush;

                return brush;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
