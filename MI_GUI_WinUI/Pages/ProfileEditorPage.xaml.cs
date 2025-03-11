using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using System;
using System.Linq;
using System.Collections.Specialized;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ProfileEditorPage : Page
    {
        private const float DROPPED_IMAGE_SIZE = 80;
        private readonly ProfileEditorViewModel ViewModel;
        private ResizableImage? activeImage;
        private Point originalPosition;
        private Canvas? EditorCanvasElement => FindName("EditorCanvas") as Canvas;
        private bool _isAddingFromDrop;
        
        public ProfileEditorPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<ProfileEditorViewModel>();
            DataContext = ViewModel;

            ViewModel.CanvasElements.CollectionChanged += CanvasElements_CollectionChanged;
            Loaded += ProfileEditorPage_Loaded;

            ViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ProfileEditorViewModel.ProfileName))
                {
                    ClearCanvas();
                }
            };

            ViewModel.NewProfile();
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
                        //await ViewModel.LoadExistingProfile(profile);
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

        private void CanvasElements_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearCanvas();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && !_isAddingFromDrop)
            {
                foreach (UnifiedPositionInfo elementInfo in e.NewItems)
                {
                    AddElementToCanvas(elementInfo);
                }
            }
        }

        private void AddElementToCanvas(UnifiedPositionInfo elementInfo)
        {
            if (EditorCanvasElement == null) return;

            var image = new ResizableImage();
            image.Width = elementInfo.Size.Width;
            image.Height = elementInfo.Size.Height;
            
            // Ensure URI is properly created, with proper error handling
            try {
                // Use the absolute URI from the element skin
                image.Source = new BitmapImage(new Uri(elementInfo.Element.Skin));
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error creating image source: {ex.Message} for path: {elementInfo.Element.Skin}");
                return;
            }
            
            image.Tag = elementInfo;
            image.ManipulationMode = ManipulationModes.All;
            image.Opacity = 1.0;
            
            // Attach manipulation events
            image.ManipulationStarted += Image_ManipulationStarted;
            image.ManipulationDelta += Image_ManipulationDelta;
            image.RightTapped += Element_RightTapped;

            // Set z-index to ensure visibility - important!
            Canvas.SetZIndex(image, 10); // Higher value to ensure visibility

            // Create context menu
            var menu = new MenuFlyout();
            var configureItem = new MenuFlyoutItem { Text = "Configure Action" };
            var deleteItem = new MenuFlyoutItem { Text = "Delete" };
            
            configureItem.Click += async (s, e) => 
            {
                // Get the current position of the image
                var imageX = Canvas.GetLeft(image);
                var imageY = Canvas.GetTop(image);

                // Find the current element info from CanvasElements based on position
                var currentElementInfo = ViewModel.CanvasElements.FirstOrDefault(e => 
                    Math.Abs(e.Position.X - imageX) < 1 && 
                    Math.Abs(e.Position.Y - imageY) < 1);

                if (currentElementInfo != null)
                {
                    await ViewModel.ConfigureAction(currentElementInfo);
                }
            };

            deleteItem.Click += (s, e) =>
            {
                // Similarly, find current element for deletion
                var imageX = Canvas.GetLeft(image);
                var imageY = Canvas.GetTop(image);
                
                var currentElementInfo = ViewModel.CanvasElements.FirstOrDefault(e => 
                    Math.Abs(e.Position.X - imageX) < 1 && 
                    Math.Abs(e.Position.Y - imageY) < 1);

                if (currentElementInfo != null)
                {
                    ViewModel.CanvasElements.Remove(currentElementInfo);
                    EditorCanvasElement.Children.Remove(image);
                }
            };

            menu.Items.Add(configureItem);
            menu.Items.Add(deleteItem);
            image.ContextFlyout = menu;

            // Set position
            Canvas.SetLeft(image, elementInfo.Position.X);
            Canvas.SetTop(image, elementInfo.Position.Y);

            // Add to canvas
            EditorCanvasElement.Children.Add(image);
            
            // Add animation for better visibility
            var scaleTransform = new ScaleTransform();
            image.RenderTransform = scaleTransform;
            
            var storyboard = new Storyboard();
            var scaleXAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };
            var scaleYAnim = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };
            
            Storyboard.SetTarget(scaleXAnim, scaleTransform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            Storyboard.SetTarget(scaleYAnim, scaleTransform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
            
            storyboard.Children.Add(scaleXAnim);
            storyboard.Children.Add(scaleYAnim);
            storyboard.Begin();
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Added element at ({elementInfo.Position.X}, {elementInfo.Position.Y}) with image source: {elementInfo.Element.Skin}");
        }

        private void Image_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            if (sender is Image sourceImage)
            {
                string sourcePath = ((BitmapImage)sourceImage.Source).UriSource.ToString();
                string elementId = sourceImage.Tag?.ToString() ?? "";

                e.Data.SetText(elementId);
                e.Data.Properties.Title = "GUI Element";
                e.Data.Properties.Add("ImagePath", sourcePath);
                e.Data.Properties.Add("ElementType", elementId.EndsWith(".py") ? "Pose" : "Button");
                e.AllowedOperations = DataPackageOperation.Copy;
            }
        }

        private void EditorCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                
                if (e.DragUIOverride != null)
                {
                    e.DragUIOverride.Caption = "Drop to add element";
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

                string elementId = await e.DataView.GetTextAsync();
                string imagePath = "";
                string elementType = "Button";
                
                if (e.DataView.Properties.TryGetValue("ImagePath", out object path))
                {
                    imagePath = path.ToString();
                }
                if (e.DataView.Properties.TryGetValue("ElementType", out object type))
                {
                    elementType = type.ToString();
                }

                if (string.IsNullOrEmpty(imagePath)) return;

                // Get the position relative to the canvas
                Point dropPosition = e.GetPosition(EditorCanvasElement);
                
                // Ensure position stays within canvas bounds - apply centering logic
                dropPosition.X = Math.Max(DROPPED_IMAGE_SIZE/2, Math.Min(dropPosition.X, 640 - DROPPED_IMAGE_SIZE/2));
                dropPosition.Y = Math.Max(DROPPED_IMAGE_SIZE/2, Math.Min(dropPosition.Y, 480 - DROPPED_IMAGE_SIZE/2));

                ElementAddRequest request;
                if (elementType == "Pose")
                {
                    request = ElementAddRequest.CreatePoseRequest(dropPosition, (int)(DROPPED_IMAGE_SIZE / 2));
                }
                else
                {
                    // The key is to use the full image path from drag data
                    request = ElementAddRequest.CreateButtonRequest(imagePath, dropPosition, (int)(DROPPED_IMAGE_SIZE / 2));
                }

                try
                {
                    _isAddingFromDrop = true;
                    
                    // Convert element skin to full path for display
                    var elementWithDisplayPath = request.Element.WithSkin(Utils.FileNameHelper.GetFullAssetPath(request.Element.Skin));
                    
                    // Create the position info with proper positioning
                    var elementInfo = new UnifiedPositionInfo(
                        elementWithDisplayPath,
                        new Point(
                            dropPosition.X - DROPPED_IMAGE_SIZE/2, // Calculate top-left from center
                            dropPosition.Y - DROPPED_IMAGE_SIZE/2
                        ),
                        new Size(DROPPED_IMAGE_SIZE, DROPPED_IMAGE_SIZE)
                    );
                    
                    // Add to canvas UI first
                    AddElementToCanvas(elementInfo);

                    ViewModel.AddButtonToCanvasCommand.Execute(elementInfo);
                }
                finally
                {
                    _isAddingFromDrop = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drop error: {ex.Message}");
                if (ViewModel.XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError($"Failed to add element to canvas: {ex.Message}", ViewModel.XamlRoot);
                }
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
            if (activeImage == null || !(activeImage.Tag is UnifiedPositionInfo elementInfo)) return;

            var newX = originalPosition.X + e.Cumulative.Translation.X;
            var newY = originalPosition.Y + e.Cumulative.Translation.Y;

            newX = Math.Max(0, Math.Min(newX, 640 - activeImage.ActualWidth));
            newY = Math.Max(0, Math.Min(newY, 480 - activeImage.ActualHeight));

            newX = Math.Round(newX);
            newY = Math.Round(newY);

            Point newPosition = new Point(newX, newY);

            Canvas.SetLeft(activeImage, newPosition.X);
            Canvas.SetTop(activeImage, newPosition.Y);

            var updatedInfo = elementInfo.With(position: newPosition);
            ViewModel.UpdateElementPosition(updatedInfo);
        }

        private void Element_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.ContextFlyout != null)
            {
                element.ContextFlyout.ShowAt(element);
            }
        }

        private void ProfileEditorPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.XamlRoot = XamlRoot;
            }
        }

        private void ClearCanvas()
        {
            if (EditorCanvasElement != null)
            {
                EditorCanvasElement.Children.Clear();
            }
        }
    }
}
