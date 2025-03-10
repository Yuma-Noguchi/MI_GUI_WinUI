using Microsoft.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ProfileEditorPage : Microsoft.UI.Xaml.Controls.Page
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

            // Subscribe to collection changes and set XamlRoot
            ViewModel.CanvasButtons.CollectionChanged += CanvasButtons_CollectionChanged;
            ViewModel.CanvasPoses.CollectionChanged += CanvasPoses_CollectionChanged;
            
            // Set XamlRoot when page is loaded
            Loaded += ProfileEditorPage_Loaded;

            // Set up collection changed handler for loading profiles
            ViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ProfileEditorViewModel.ProfileName))
                {
                    ClearCanvas();
                }
            };

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

        private void CanvasPoses_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearCanvas();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && !_isAddingFromDrop)
            {
                foreach (PosePositionInfo poseInfo in e.NewItems)
                {
                    AddPoseToCanvas(poseInfo);
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

        private void AddButtonToCanvasWithAnimation(ResizableImage image)
        {
            var scaleTransform = new ScaleTransform();
            image.RenderTransform = scaleTransform;

            var storyboard = CreateDropAnimation(scaleTransform);
            storyboard.Begin();
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
                e.Data.Properties.Add("Type", "Button");
                e.AllowedOperations = DataPackageOperation.Copy;
            }
        }

        private void Pose_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            if (sender is Image sourceImage)
            {
                string sourcePath = ((BitmapImage)sourceImage.Source).UriSource.ToString();
                string poseFile = sourceImage.Tag?.ToString() ?? "";

                e.Data.SetText(poseFile);
                e.Data.Properties.Title = "Pose Element";
                e.Data.Properties.Add("ImagePath", sourcePath);
                e.Data.Properties.Add("Type", "Pose");
                e.AllowedOperations = DataPackageOperation.Copy;
            }
        }

        private void Pose_DropCompleted(UIElement sender, DropCompletedEventArgs e)
        {
            // Clean up if needed after drag operation
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

                string elementType = "Button";
                if (e.DataView.Properties.TryGetValue("Type", out object type))
                {
                    elementType = type.ToString();
                }

                string elementId = await e.DataView.GetTextAsync();
                string imagePath = "";
                
                if (e.DataView.Properties.TryGetValue("ImagePath", out object path))
                {
                    imagePath = path.ToString();
                }

                if (string.IsNullOrEmpty(imagePath)) return;

                // Get the position relative to the canvas
                Point dropPosition = e.GetPosition(EditorCanvasElement);
                
                // Ensure position stays within canvas bounds
                dropPosition.X = Math.Max(DROPPED_IMAGE_SIZE/2, Math.Min(dropPosition.X, 640 - DROPPED_IMAGE_SIZE/2));
                dropPosition.Y = Math.Max(DROPPED_IMAGE_SIZE/2, Math.Min(dropPosition.Y, 480 - DROPPED_IMAGE_SIZE/2));

                if (elementType == "Button")
                {
                    HandleButtonDrop(elementId, imagePath, dropPosition);
                }
                else if (elementType == "Pose")
                {
                    HandlePoseDrop(elementId, imagePath, dropPosition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Drop error: {ex.Message}");
            }
        }

        private void HandleButtonDrop(string buttonType, string imagePath, Point dropPosition)
        {
            var sourceButton = FindSourceButton(buttonType);
            if (sourceButton == null) return;

            var buttonInfo = new ButtonPositionInfo
            {
                Button = sourceButton.Clone(),
                Position = new Point(
                    Math.Round(dropPosition.X - DROPPED_IMAGE_SIZE / 2),
                    Math.Round(dropPosition.Y - DROPPED_IMAGE_SIZE / 2)
                ),
                Size = new Size(DROPPED_IMAGE_SIZE, DROPPED_IMAGE_SIZE)
            };

            AddButtonToCanvas(buttonInfo);

            if (EditorCanvasElement?.Children.LastOrDefault() is ResizableImage lastImage)
            {
                AddButtonToCanvasWithAnimation(lastImage);
            }

            _isAddingFromDrop = true;
            ViewModel.AddButtonToCanvasCommand.Execute(buttonInfo);
            _isAddingFromDrop = false;
        }

        private void HandlePoseDrop(string poseFile, string imagePath, Point dropPosition)
        {
            var sourcePose = ViewModel.DefaultPoses.FirstOrDefault(p => p.File.Equals(poseFile));
            if (sourcePose.File == null) return;

            var poseInfo = new PosePositionInfo
            {
                Pose = new PoseGuiElement
                {
                    File = sourcePose.File,
                    LeftSkin = sourcePose.LeftSkin,
                    RightSkin = sourcePose.RightSkin,
                    Sensitivity = sourcePose.Sensitivity,
                    Deadzone = sourcePose.Deadzone,
                    Linear = sourcePose.Linear,
                    Flag = sourcePose.Flag,
                    Landmark = sourcePose.Landmark,
                    Position = new List<int> { (int)dropPosition.X, (int)dropPosition.Y },
                    Radius = sourcePose.Radius,
                    Skin = sourcePose.Skin,
                    Action = sourcePose.Action
                },
                Position = new Point(
                    Math.Round(dropPosition.X - DROPPED_IMAGE_SIZE / 2),
                    Math.Round(dropPosition.Y - DROPPED_IMAGE_SIZE / 2)
                ),
                Size = new Size(DROPPED_IMAGE_SIZE, DROPPED_IMAGE_SIZE)
            };

            AddPoseToCanvas(poseInfo);

            if (EditorCanvasElement?.Children.LastOrDefault() is ResizableImage lastImage)
            {
                AddButtonToCanvasWithAnimation(lastImage);
            }

            _isAddingFromDrop = true;
            ViewModel.AddPoseToCanvasCommand.Execute(poseInfo);
            _isAddingFromDrop = false;
        }

        private void AddPoseToCanvas(PosePositionInfo poseInfo)
        {
            if (EditorCanvasElement == null) return;

            var image = new ResizableImage
            {
                Source = new BitmapImage(new Uri($"ms-appx:///MotionInput/data/assets/{poseInfo.Pose.Skin}")),
                Width = poseInfo.Size.Width,
                Height = poseInfo.Size.Height,
                Tag = poseInfo.Pose.File,
                ManipulationMode = ManipulationModes.All
            };

            Canvas.SetLeft(image, poseInfo.Position.X);
            Canvas.SetTop(image, poseInfo.Position.Y);

            EditorCanvasElement.Children.Add(image);

            image.ManipulationStarted += Image_ManipulationStarted;
            image.ManipulationDelta += Image_ManipulationDelta;
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

            // Calculate new position
            var newX = originalPosition.X + e.Cumulative.Translation.X;
            var newY = originalPosition.Y + e.Cumulative.Translation.Y;

            // Ensure the center of the button stays within canvas bounds
            var radius = activeImage.ActualWidth / 2;
            newX = Math.Max(radius, Math.Min(newX + radius, 640 - radius)) - radius;
            newY = Math.Max(radius, Math.Min(newY + radius, 480 - radius)) - radius;

            // Round to nearest pixel for grid alignment
            newX = Math.Round(newX);
            newY = Math.Round(newY);

            Point newPosition = new Point(newX, newY);

            // Update visual position
            Canvas.SetLeft(activeImage, newPosition.X);
            Canvas.SetTop(activeImage, newPosition.Y);

            // Update model
            if (activeImage.Tag is string elementType)
            {
                var sourceButton = FindSourceButton(elementType);
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
                else
                {
                    var sourcePose = ViewModel.DefaultPoses.FirstOrDefault(p => p.File.Equals(elementType));
                    if (sourcePose.File != null)
                    {
                        var poseInfo = new PosePositionInfo
                        {
                            Pose = new PoseGuiElement
                            {
                                File = sourcePose.File,
                                LeftSkin = sourcePose.LeftSkin,
                                RightSkin = sourcePose.RightSkin,
                                Sensitivity = sourcePose.Sensitivity,
                                Deadzone = sourcePose.Deadzone,
                                Linear = sourcePose.Linear,
                                Flag = sourcePose.Flag,
                                Landmark = sourcePose.Landmark,
                                Position = new List<int> { (int)newPosition.X, (int)newPosition.Y },
                                Radius = sourcePose.Radius,
                                Skin = sourcePose.Skin,
                                Action = sourcePose.Action
                            },
                            Position = newPosition,
                            Size = new Size(activeImage.ActualWidth, activeImage.ActualHeight)
                        };
                        ViewModel.UpdatePosePosition(poseInfo);
                    }
                }
            }
        }

        private EditorButton? FindSourceButton(string buttonType)
        {
            return ViewModel.DefaultButtons.FirstOrDefault(b => b.Name == buttonType) ??
                   ViewModel.CustomButtons.FirstOrDefault(b => b.Name == buttonType);
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
