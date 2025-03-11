using Microsoft.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;

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

                if (e.Parameter is Profile profile)
                {
                    try
                    {
                        await ViewModel.LoadProfile(profile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading profile: {ex.Message}");
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

        private void ClearCanvas()
        {
            if (EditorCanvasElement != null)
            {
                EditorCanvasElement.Children.Clear();
            }
        }

        private void AddElementWithAnimation(ResizableImage image)
        {
            var scaleTransform = new ScaleTransform();
            image.RenderTransform = scaleTransform;

            var storyboard = CreateDropAnimation(scaleTransform);
            storyboard.Begin();
        }

        private void AddElementToCanvas(UnifiedPositionInfo elementInfo)
        {
            if (EditorCanvasElement == null) return;

            var image = new ResizableImage
            {
                Source = new BitmapImage(new Uri(elementInfo.Element.Skin)),
                Width = elementInfo.Size.Width,
                Height = elementInfo.Size.Height,
                Tag = elementInfo,
                ManipulationMode = ManipulationModes.All
            };

            Canvas.SetLeft(image, elementInfo.Position.X);
            Canvas.SetTop(image, elementInfo.Position.Y);

            EditorCanvasElement.Children.Add(image);

            image.ManipulationStarted += Image_ManipulationStarted;
            image.ManipulationDelta += Image_ManipulationDelta;
            image.Tapped += Element_Tapped;
            image.RightTapped += Element_RightTapped;

            AddElementWithAnimation(image);
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
                    e.DragUIOverride.Caption = "Drop to place element";
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
                
                if (e.DataView.Properties.TryGetValue("ImagePath", out object path))
                {
                    imagePath = path.ToString();
                }

                if (string.IsNullOrEmpty(imagePath)) return;

                Point dropPosition = e.GetPosition(EditorCanvasElement);
                dropPosition.X = Math.Max(0, Math.Min(dropPosition.X, 640 - DROPPED_IMAGE_SIZE));
                dropPosition.Y = Math.Max(0, Math.Min(dropPosition.Y, 480 - DROPPED_IMAGE_SIZE));

                ElementAddRequest request;
                if (elementId.EndsWith(".py"))
                {
                    request = ElementAddRequest.CreatePoseRequest(dropPosition, (int)(DROPPED_IMAGE_SIZE / 2));
                }
                else
                {
                    request = ElementAddRequest.CreateButtonRequest(imagePath, dropPosition, (int)(DROPPED_IMAGE_SIZE / 2));
                }

                try
                {
                    _isAddingFromDrop = true;
                    ViewModel.AddElementToCanvas(request);
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
                    await Utils.DialogHelper.ShowError("Failed to add element to canvas.", ViewModel.XamlRoot);
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

            var radius = activeImage.ActualWidth / 2;
            newX = Math.Max(0, Math.Min(newX + radius, 640 - activeImage.ActualWidth));
            newY = Math.Max(0, Math.Min(newY + radius, 480 - activeImage.ActualHeight));

            newX = Math.Round(newX);
            newY = Math.Round(newY);

            Point newPosition = new Point(newX, newY);

            Canvas.SetLeft(activeImage, newPosition.X);
            Canvas.SetTop(activeImage, newPosition.Y);

            var updatedInfo = elementInfo.Clone();
            updatedInfo.Position = newPosition;
            ViewModel.UpdateElementPosition(updatedInfo);
        }

        private void Element_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is UnifiedPositionInfo elementInfo)
            {
                // Handle element selection if needed
            }
        }

        private void Element_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }

        private async void ConfigureAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.DataContext is UnifiedPositionInfo elementInfo)
            {
                await ViewModel.ConfigureAction(elementInfo);
            }
        }

        private void DeleteElement_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.DataContext is UnifiedPositionInfo elementInfo)
            {
                ViewModel.CanvasElements.Remove(elementInfo);
                
                if (EditorCanvasElement != null)
                {
                    var imageToRemove = EditorCanvasElement.Children
                        .OfType<ResizableImage>()
                        .FirstOrDefault(img => img.Tag == elementInfo);
                        
                    if (imageToRemove != null)
                    {
                        EditorCanvasElement.Children.Remove(imageToRemove);
                    }
                }
            }
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
