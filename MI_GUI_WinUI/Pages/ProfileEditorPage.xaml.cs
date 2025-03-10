using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Storage;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ProfileEditorPage : Microsoft.UI.Xaml.Controls.Page
    {
        private const float DROPPED_IMAGE_SIZE = 80;
        private readonly ProfileEditorViewModel ViewModel;
        private ResizableImage? activeImage;
        private Point originalPosition;
        private Canvas? EditorCanvasElement => FindName("EditorCanvas") as Canvas;
        private string? _lastDroppedPath;
        private bool _isAddingFromDrop;
        
        public ProfileEditorPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<ProfileEditorViewModel>();
            DataContext = ViewModel;

            // Subscribe to collection changes in CanvasButtons
            ViewModel.CanvasButtons.CollectionChanged += CanvasButtons_CollectionChanged;
            
            // Set XamlRoot when page is loaded
            Loaded += ProfileEditorPage_Loaded;

            // Ensure clean state for new profile creation
            ViewModel.NewProfile();
        }

        private void ProfileEditorPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = XamlRoot;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = XamlRoot;

                // Load profile if one was passed during navigation
                if (e.Parameter is Profile profile)
                {
                    try
                    {
                        await ViewModel.LoadExistingProfile(profile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading profile: {ex.Message}");
                        // Fall back to new profile if loading fails
                        ViewModel.NewProfile();
                    }
                }
            }
        }

        private void CanvasButtons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearCanvas();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && !_isAddingFromDrop)
            {
                foreach (ButtonPositionInfo buttonInfo in e.NewItems)
                {
                    AddButtonToCanvas(buttonInfo);
                }
            }
        }

        private void ClearCanvas()
        {
            if (EditorCanvasElement != null)
            {
                EditorCanvasElement.Children.Clear();
            }
        }

        private async Task AddButtonToCanvasWithAnimation(ResizableImage image)
        {
            var scaleTransform = new ScaleTransform();
            image.RenderTransform = scaleTransform;

            var storyboard = CreateDropAnimation(scaleTransform);
            storyboard.Begin();

            if (_lastDroppedPath != null)
            {
                await SwitchToTriggeredState(image, _lastDroppedPath);
                _lastDroppedPath = null;
            }
        }

        private void AddButtonToCanvas(ButtonPositionInfo buttonInfo)
        {
            if (EditorCanvasElement == null) return;

            var image = new ResizableImage
            {
                Source = new BitmapImage(new Uri(buttonInfo.Button.IconPath)),
                Width = buttonInfo.Size.Width,
                Height = buttonInfo.Size.Height,
                Tag = buttonInfo.Button.Name,
                ManipulationMode = ManipulationModes.All
            };

            Canvas.SetLeft(image, buttonInfo.Position.X);
            Canvas.SetTop(image, buttonInfo.Position.Y);

            EditorCanvasElement.Children.Add(image);

            // Add manipulation events
            image.ManipulationStarted += Image_ManipulationStarted;
            image.ManipulationDelta += Image_ManipulationDelta;
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
                if (!e.DataView.Contains(StandardDataFormats.Text)) return;

                string buttonType = await e.DataView.GetTextAsync();
                string imagePath = "";
                
                if (e.DataView.Properties.TryGetValue("ImagePath", out object path))
                {
                    imagePath = path.ToString();
                }

                if (string.IsNullOrEmpty(imagePath)) return;

                Point dropPosition = e.GetPosition((UIElement)sender);
                _lastDroppedPath = imagePath;

                // Find the source button from DefaultButtons or CustomButtons
                var sourceButton = FindSourceButton(buttonType);
                if (sourceButton == null) return;

                var buttonInfo = new ButtonPositionInfo
                {
                    Button = sourceButton.Clone(),
                    Position = new Point(dropPosition.X - DROPPED_IMAGE_SIZE / 2, dropPosition.Y - DROPPED_IMAGE_SIZE / 2),
                    Size = new Size(DROPPED_IMAGE_SIZE, DROPPED_IMAGE_SIZE)
                };

                // Add the button to canvas first
                AddButtonToCanvas(buttonInfo);

                // Get the last added image and apply animation
                if (EditorCanvasElement?.Children.LastOrDefault() is ResizableImage lastImage)
                {
                    await AddButtonToCanvasWithAnimation(lastImage);
                }

                // Now add to the ViewModel's collection (which won't trigger another visual update due to _isAddingFromDrop)
                _isAddingFromDrop = true;
                ViewModel.AddButtonToCanvasCommand.Execute(buttonInfo);
                _isAddingFromDrop = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drop error: {ex.Message}");
            }
        }

        private void Image_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (sender is ResizableImage image)
            {
                activeImage = image;
                originalPosition = new Point(Canvas.GetLeft(image), Canvas.GetTop(image));
            }
        }

        private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (activeImage == null) return;

            Point newPosition = new Point(
                originalPosition.X + e.Cumulative.Translation.X,
                originalPosition.Y + e.Cumulative.Translation.Y
            );

            // Update visual position
            Canvas.SetLeft(activeImage, newPosition.X);
            Canvas.SetTop(activeImage, newPosition.Y);

            // Update model
            if (activeImage.Tag is string buttonType)
            {
                var sourceButton = FindSourceButton(buttonType);
                if (sourceButton != null)
                {
                    var buttonInfo = new ButtonPositionInfo
                    {
                        Button = sourceButton.Clone(),
                        Position = newPosition,
                        Size = new Size(activeImage.ActualWidth, activeImage.ActualHeight)
                    };
                    ViewModel.UpdateButtonPosition(buttonInfo);
                }
            }
        }

        private EditorButton? FindSourceButton(string buttonType)
        {
            return ViewModel.DefaultButtons.FirstOrDefault(b => b.Name == buttonType) ??
                   ViewModel.CustomButtons.FirstOrDefault(b => b.Name == buttonType);
        }

        private async Task SwitchToTriggeredState(ResizableImage image, string imagePath)
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
