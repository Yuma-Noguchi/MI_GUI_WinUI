using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class ResizableImage : UserControl
    {
        public const double MIN_SIZE = 40;
        public const double MAX_SIZE = 120;
        
        private bool isResizing;
        private Point startPosition;
        private Size originalSize;
        private bool isPointerOver;
        
        public ResizableImage()
        {
            this.InitializeComponent();
            
            // Setup pointer events for grip visibility
            this.PointerEntered += OnPointerEntered;
            this.PointerExited += OnPointerExited;
            
            if (TopLeftGrip != null)
            {
                TopLeftGrip.ManipulationStarted += OnGripManipulationStarted;
                TopLeftGrip.ManipulationDelta += OnGripManipulationDelta;
                TopLeftGrip.ManipulationCompleted += OnGripManipulationCompleted;
            }
            
            if (BottomRightGrip != null)
            {
                BottomRightGrip.ManipulationStarted += OnGripManipulationStarted;
                BottomRightGrip.ManipulationDelta += OnGripManipulationDelta;
                BottomRightGrip.ManipulationCompleted += OnGripManipulationCompleted;
            }
        }

        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(ImageSource),
                typeof(ResizableImage),
                new PropertyMetadata(null, OnSourceChanged)
            );

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ResizableImage control && control.MainImage != null)
            {
                control.MainImage.Source = (ImageSource?)e.NewValue;
            }
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            isPointerOver = true;
            if (!isResizing && TopLeftGrip != null && BottomRightGrip != null)
            {
                TopLeftGrip.Visibility = Visibility.Visible;
                BottomRightGrip.Visibility = Visibility.Visible;
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            isPointerOver = false;
            if (!isResizing && TopLeftGrip != null && BottomRightGrip != null)
            {
                TopLeftGrip.Visibility = Visibility.Collapsed;
                BottomRightGrip.Visibility = Visibility.Collapsed;
            }
        }

        private void OnGripManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            isResizing = true;
            startPosition = new Point(ActualWidth, ActualHeight);
            originalSize = new Size(ActualWidth, ActualHeight);
        }

        private void OnGripManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!isResizing) return;

            var newWidth = originalSize.Width;
            var newHeight = originalSize.Height;

            if (sender == TopLeftGrip)
            {
                newWidth = originalSize.Width - e.Cumulative.Translation.X;
                newHeight = originalSize.Height - e.Cumulative.Translation.Y;
            }
            else if (sender == BottomRightGrip)
            {
                newWidth = originalSize.Width + e.Cumulative.Translation.X;
                newHeight = originalSize.Height + e.Cumulative.Translation.Y;
            }

            // Keep aspect ratio
            var ratio = originalSize.Width / originalSize.Height;
            if (Math.Abs(newWidth - startPosition.X) > Math.Abs(newHeight - startPosition.Y))
            {
                newHeight = newWidth / ratio;
            }
            else
            {
                newWidth = newHeight * ratio;
            }

            // Apply size constraints
            Width = Math.Clamp(newWidth, MIN_SIZE, MAX_SIZE);
            Height = Math.Clamp(newHeight, MIN_SIZE, MAX_SIZE);
        }

        private void OnGripManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            isResizing = false;
            if (!isPointerOver && TopLeftGrip != null && BottomRightGrip != null)
            {
                TopLeftGrip.Visibility = Visibility.Collapsed;
                BottomRightGrip.Visibility = Visibility.Collapsed;
            }
        }
    }
}
