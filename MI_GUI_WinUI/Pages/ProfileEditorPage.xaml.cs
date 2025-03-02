using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using Windows.Storage;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ProfileEditorPage : Page
    {
        private const float DROPPED_IMAGE_SIZE = 80;
        private readonly ProfileEditorViewModel ViewModel;
        
        public ProfileEditorPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetService<ProfileEditorViewModel>();
            DataContext = ViewModel;
        }

        private void Image_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            if (sender is Image sourceImage)
            {
                // Get the image path from source
                string sourcePath = ((BitmapImage)sourceImage.Source).UriSource.ToString();
                
                // Store image data for transfer
                e.Data.SetText(sourceImage.Tag?.ToString() ?? "");
                e.Data.Properties.Title = "Xbox Button";
                e.Data.Properties.Add("ImagePath", sourcePath);
                e.AllowedOperations = DataPackageOperation.Copy;
            }
        }

        private void Image_DropCompleted(UIElement sender, DropCompletedEventArgs e)
        {
            // Clean up if needed after drag operation
        }

        private void EditorCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                
                if (e.DragUIOverride != null)
                {
                    e.DragUIOverride.Caption = "Drop to place button";
                    e.DragUIOverride.IsCaptionVisible = true;
                    e.DragUIOverride.IsGlyphVisible = false;
                    e.DragUIOverride.IsContentVisible = true;
                }
            }
        }

        private async void EditorCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.DataView.Contains(StandardDataFormats.Text))
                {
                    string buttonType = await e.DataView.GetTextAsync();
                    string imagePath = "";
                    
                    if (e.DataView.Properties.TryGetValue("ImagePath", out object path))
                    {
                        imagePath = path.ToString();
                    }

                    if (string.IsNullOrEmpty(imagePath))
                        return;

                    Point dropPosition = e.GetPosition((UIElement)sender);

                    // Create new image with direct path
                    var newImage = new Image
                    {
                        Source = new BitmapImage(new Uri(imagePath)),
                        Width = DROPPED_IMAGE_SIZE,
                        Height = DROPPED_IMAGE_SIZE,
                        Tag = buttonType,
                        CanDrag = true,
                        AllowDrop = false
                    };

                    // Center on drop position
                    Canvas.SetLeft(newImage, dropPosition.X - DROPPED_IMAGE_SIZE / 2);
                    Canvas.SetTop(newImage, dropPosition.Y - DROPPED_IMAGE_SIZE / 2);

                    if (sender is Canvas canvas)
                    {
                        // Add to canvas immediately
                        canvas.Children.Add(newImage);

                        // Setup transform and animation
                        var scaleTransform = new ScaleTransform();
                        newImage.RenderTransform = scaleTransform;

                        var storyboard = CreateDropAnimation(scaleTransform);
                        storyboard.Begin();

                        // Handle triggered state
                        await SwitchToTriggeredState(newImage, imagePath);

                        // Find the source button from DefaultButtons or CustomButtons
                        var sourceButton = FindSourceButton(buttonType);
                        if (sourceButton != null)
                        {
                            var buttonInfo = new ButtonPositionInfo
                            {
                                Button = sourceButton.Clone(),
                                Position = dropPosition
                            };
                            ViewModel.AddButtonToCanvasCommand.Execute(buttonInfo);
                        }

                        // Attach drag events
                        newImage.DragStarting += Image_DragStarting;
                        newImage.DropCompleted += Image_DropCompleted;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drop error: {ex.Message}");
            }
        }

        private EditorButton? FindSourceButton(string buttonType)
        {
            return ViewModel.DefaultButtons.FirstOrDefault(b => b.Name == buttonType) ??
                   ViewModel.CustomButtons.FirstOrDefault(b => b.Name == buttonType);
        }

        private async Task SwitchToTriggeredState(Image image, string imagePath)
        {
            string triggeredPath = imagePath.Replace(".png", "_triggered.png");
            await Task.Delay(100);
            image.Source = new BitmapImage(new Uri(triggeredPath));
            await Task.Delay(200);
            image.Source = new BitmapImage(new Uri(imagePath));
        }

        private Storyboard CreateDropAnimation(ScaleTransform transform)
        {
            var storyboard = new Storyboard();
            var scaleXAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new ElasticEase { Oscillations = 1 }
            };
            var scaleYAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new ElasticEase { Oscillations = 1 }
            };

            Storyboard.SetTarget(scaleXAnim, transform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            Storyboard.SetTarget(scaleYAnim, transform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

            storyboard.Children.Add(scaleXAnim);
            storyboard.Children.Add(scaleYAnim);
            return storyboard;
        }
    }
}
