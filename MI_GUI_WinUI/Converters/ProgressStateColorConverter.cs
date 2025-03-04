using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using MI_GUI_WinUI.Models;
using Windows.UI;
using System;

namespace MI_GUI_WinUI.Converters
{
    public class ProgressStateColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 128, 128, 128));  // Gray
        private static readonly SolidColorBrush InitBrush = new(Color.FromArgb(255, 64, 128, 255));      // Light Blue
        private static readonly SolidColorBrush ProcessingBrush = new(Color.FromArgb(255, 0, 120, 215)); // Blue
        private static readonly SolidColorBrush GeneratingBrush = new(Color.FromArgb(255, 0, 183, 195)); // Cyan
        private static readonly SolidColorBrush CompletedBrush = new(Color.FromArgb(255, 16, 185, 129)); // Green
        private static readonly SolidColorBrush ErrorBrush = new(Color.FromArgb(255, 231, 76, 60));      // Red
        private static readonly SolidColorBrush CancelledBrush = new(Color.FromArgb(255, 243, 156, 18)); // Orange

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not GenerationProgressState state)
                return DefaultBrush;

            return state switch
            {
                GenerationProgressState.NotStarted => DefaultBrush,
                GenerationProgressState.Initializing => InitBrush,
                GenerationProgressState.Loading => InitBrush,
                GenerationProgressState.Tokenizing => ProcessingBrush,
                GenerationProgressState.Encoding => ProcessingBrush,
                GenerationProgressState.InitializingLatents => ProcessingBrush,
                GenerationProgressState.Generating => GeneratingBrush,
                GenerationProgressState.Diffusing => GeneratingBrush,
                GenerationProgressState.Decoding => ProcessingBrush,
                GenerationProgressState.Finalizing => ProcessingBrush,
                GenerationProgressState.Completing => CompletedBrush,
                GenerationProgressState.Completed => CompletedBrush,
                GenerationProgressState.Failed => ErrorBrush,
                GenerationProgressState.Cancelled => CancelledBrush,
                _ => DefaultBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
