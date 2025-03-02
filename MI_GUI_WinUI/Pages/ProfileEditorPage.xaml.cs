using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using MI_GUI_WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Models;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Microsoft.UI;
using Windows.ApplicationModel.DataTransfer;
using System.Text.Json;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Dispatching;

namespace MI_GUI_WinUI.Pages
{
    public sealed partial class ProfileEditorPage : Page
    {
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is T typedParent) return typedParent;
            return FindParent<T>(parent);
        }

        private ProfileEditorViewModel? _viewModel;
        private Canvas? _backgroundGrid;
        private Button? _canvasArea;

        public ProfileEditorViewModel? ViewModel
        {
            get => _viewModel;
            private set
            {
                if (_viewModel != value)
                {
                    _viewModel = value;
                    this.DataContext = _viewModel;
                }
            }
        }

        private void SetViewModel(ProfileEditorViewModel viewModel)
        {
            ViewModel = viewModel;
            if (Application.Current is App app)
            {
                var windowManager = app.Services.GetRequiredService<WindowManager>();
                ViewModel.Window = windowManager.MainWindow;
            }
            Debug.WriteLine($"ViewModel set with {viewModel.DefaultButtons.Count} default buttons");
        }

        private const int GridSize = 20;
        private DraggableGuiElement? _currentDraggedElement;
        private Point _dragStartPoint;
        private bool _isDragging;
        private bool _isPointerCaptured;

        public ProfileEditorPage()
        {
            this.InitializeComponent();
            
            // Get ViewModel from DI
            var vm = Ioc.Default.GetRequiredService<ProfileEditorViewModel>();
            SetViewModel(vm);
            
            this.Loaded += ProfileEditorPage_Loaded;
            Debug.WriteLine("ProfileEditorPage constructed");
        }

        private void ProfileEditorPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ProfileEditorPage_Loaded called");
            
            // Find UI elements
            _backgroundGrid = FindName("BackgroundGrid") as Canvas;
            _canvasArea = FindName("CanvasArea") as Button;
            
            if (_backgroundGrid != null)
            {
                DrawGrid();
            }
        }

        private void DrawGrid()
        {
            if (_backgroundGrid == null) return;
            _backgroundGrid.Children.Clear();
            
            double width = 640;
            double height = 480;

            for (int x = 0; x <= width; x += GridSize)
            {
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 0.5,
                    Opacity = 0.3
                };
                _backgroundGrid.Children.Add(line);
            }

            for (int y = 0; y <= height; y += GridSize)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 0.5,
                    Opacity = 0.3
                };
                _backgroundGrid.Children.Add(line);
            }
        }

        private void Button_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            try
            {
                if (sender is FrameworkElement element && 
                    element.DataContext is DraggableGuiElement sourceElement)
                {
                    // Clone the source element to avoid modifying the original
                    _currentDraggedElement = sourceElement.Clone();
                    
                    Debug.WriteLine($"Starting drag for element: {_currentDraggedElement.DisplayName}");
                    args.AllowedOperations = DataPackageOperation.Copy;

                    // Serialize complete GuiElement
                    var completeData = _currentDraggedElement.ToGuiElement();
                    args.Data.SetData("application/json", JsonSerializer.Serialize(completeData));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DragStarting: {ex}");
                if (ViewModel != null)
                {
                    ViewModel.ErrorMessage = $"Error starting drag: {ex.Message}";
                }
            }
        }

        private void EditorCanvas_DragEnter(object sender, DragEventArgs args)
        {
            try
            {
                if (args.DataView.Contains("application/json"))
                {
                    VisualStateManager.GoToState(_canvasArea, "DragOver", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DragEnter: {ex}");
            }
        }

        private void EditorCanvas_DragLeave(object sender, DragEventArgs args)
        {
            try
            {
                if (_canvasArea != null)
                {
                    VisualStateManager.GoToState(_canvasArea, "Normal", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DragLeave: {ex}");
            }
        }

        private void EditorCanvas_DragOver(object sender, DragEventArgs args)
        {
            try
            {
                if (args.DataView.Contains("application/json"))
                {
                    args.AcceptedOperation = DataPackageOperation.Copy;
                }
                else
                {
                    args.AcceptedOperation = DataPackageOperation.None;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DragOver: {ex}");
                args.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private async void EditorCanvas_Drop(object sender, DragEventArgs args)
        {
            try
            {
                if (sender is Canvas canvas)
                {
                    var position = args.GetPosition(canvas);

                    if (ViewModel?.SnapToGrid ?? false)
                    {
                        position.X = Math.Round(position.X / GridSize) * GridSize;
                        position.Y = Math.Round(position.Y / GridSize) * GridSize;
                    }

                    DraggableGuiElement elementToAdd;

                    if (_currentDraggedElement != null)
                    {
                        elementToAdd = _currentDraggedElement.Clone();
                    }
                    else if (args.DataView.Contains("application/json"))
                    {
                        var json = await args.DataView.GetTextAsync("application/json");
                        var element = JsonSerializer.Deserialize<GuiElement>(json);
                        elementToAdd = new DraggableGuiElement(element);
                    }
                    else
                    {
                        return;
                    }

                    elementToAdd.X = position.X;
                    elementToAdd.Y = position.Y;

                    if (ViewModel != null)
                    {
                        ViewModel.AddCanvasElement(elementToAdd);
                        Debug.WriteLine($"Element added to canvas at ({position.X}, {position.Y}). Total elements: {ViewModel.CanvasElements.Count}");
                    }

                    args.AcceptedOperation = DataPackageOperation.Copy;
                    
                    if (_canvasArea != null)
                    {
                        VisualStateManager.GoToState(_canvasArea, "Normal", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Drop: {ex}");
                if (ViewModel != null)
                {
                    ViewModel.ErrorMessage = $"Error during drop operation: {ex.Message}";
                }
            }
            finally
            {
                _currentDraggedElement = null;
            }
        }

        private void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || 
                !(element.DataContext is DraggableGuiElement guiElement)) return;

            var canvas = FindParent<Canvas>(element);
            if (canvas == null) return;

            _dragStartPoint = e.GetCurrentPoint(canvas).Position;
            _isDragging = true;
            _isPointerCaptured = element.CapturePointer(e.Pointer);
            Debug.WriteLine($"Element pointer pressed at: ({_dragStartPoint.X}, {_dragStartPoint.Y})");
            e.Handled = true;
        }

        private void Element_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                if (!_isDragging || !_isPointerCaptured) return;

                if (sender is FrameworkElement element && 
                    element.DataContext is DraggableGuiElement guiElement)
                {
                    var canvas = FindParent<Canvas>(element);
                    if (canvas == null) return;

                    var currentPoint = e.GetCurrentPoint(canvas).Position;

                    // Calculate relative movement
                    double deltaX = currentPoint.X - _dragStartPoint.X;
                    double deltaY = currentPoint.Y - _dragStartPoint.Y;

                    // Calculate new position
                    double newX = guiElement.X + deltaX;
                    double newY = guiElement.Y + deltaY;

                    // Apply snap to grid if enabled
                    if (ViewModel?.SnapToGrid ?? false)
                    {
                        newX = Math.Round(newX / GridSize) * GridSize;
                        newY = Math.Round(newY / GridSize) * GridSize;
                    }

                    // Update element position
                    guiElement.X = newX;
                    guiElement.Y = newY;

                    // Update drag start point
                    _dragStartPoint = currentPoint;

                    Debug.WriteLine($"Element moved to: ({newX}, {newY})");
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Element_PointerMoved: {ex}");
                if (ViewModel != null)
                {
                    ViewModel.ErrorMessage = $"Error moving element: {ex.Message}";
                }
            }
        }

        private void Element_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.ReleasePointerCapture(e.Pointer);
                _isDragging = false;
                _isPointerCaptured = false;
                e.Handled = true;
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.CanvasElements.Clear();
            Debug.WriteLine("Canvas cleared");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.Help();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (e.Parameter is ProfileEditorViewModel vm)
                {
                    SetViewModel(vm);
                }

                if (ViewModel != null)
                {
                    await ViewModel.InitializeAsync();
                    Debug.WriteLine($"Profile Editor initialized with {ViewModel.CanvasElements.Count} canvas elements");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnNavigatedTo: {ex}");
                if (ViewModel != null)
                {
                    ViewModel.ErrorMessage = $"Error initializing page: {ex.Message}";
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            
            if (ViewModel != null)
            {
                ViewModel.Cleanup();
            }
            ViewModel = null;
            Debug.WriteLine("ProfileEditorPage cleanup completed");
        }
    }
}
