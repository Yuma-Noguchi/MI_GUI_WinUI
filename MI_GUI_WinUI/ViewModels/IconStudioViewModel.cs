using MI_GUI_WinUI.Common;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.ViewModels
{
    public class IconStudioViewModel : ModelBase
    {
        private bool _executingInference = false;
        private DispatcherQueue dispatcherQueue;

        public IconStudioViewModel(GeneratorModel model)
        {
            _model = model;
            _inputDescription = "landscape, painting, rolling hills, windmill, clouds";
            _numberOfImages = 1;
            _images = new ObservableCollection<ImageSource>();
            _generateCommand = new CommandBase<object>(ExecuteGenerateCommand, CanExecuteGenerateCommand);
            
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _model.PropertyChanged += _model_PropertyChanged;
            StatusString = "Idle";
        }

        private void _model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_model.Percentage):
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        ProcessPrecentageChange(_model.Percentage);
                    });
                    break;
                default:
                    break;
            }
        }

        private void ProcessPrecentageChange(double percentage)
        {
            ProgressPercentage = percentage;
        }

        private CommandBase<object> _generateCommand;

        public CommandBase<object> GenerateCommand
        {
            get => _generateCommand;
            set => SetProperty<CommandBase<object>>(ref _generateCommand, value);
        }

        private async void ExecuteGenerateCommand(object args)
        {
            _executingInference = true;
            
            dispatcherQueue.TryEnqueue(() =>
            {
                StatusString = "Generating ...";
                _generateCommand.RaiseCanExecute();
            });

            var imagePaths = await _model.GenerateImages(_inputDescription, _numberOfImages)
                .ConfigureAwait(true);

            StatusString = String.Format("{0} iterations ({1:F1} it/sec); {2:F1} sec total", 
                _model.NumInferenceSteps, _model.IterationsPerSecond, _model.LastTimeMilliseconds / 1000.0);
                
            _executingInference = false;
            _generateCommand.RaiseCanExecute();

            // create images and bind them
            await LoadImagesAsync(imagePaths);
        }

        private async Task LoadImagesAsync(IEnumerable<string> imagePaths)
        {
            var imageSources = new ObservableCollection<ImageSource>();
            
            foreach (string imagePath in imagePaths)
            {
                var bitmap = new BitmapImage();
                
                // Use WinUI's URI loading method
                Uri uri = new Uri(imagePath);
                
                // Different loading pattern for WinUI
                if (uri.Scheme == "file")
                {
                    // For local files
                    StorageFile file = await StorageFile.GetFileFromPathAsync(imagePath);
                    using (var stream = await file.OpenReadAsync())
                    {
                        await bitmap.SetSourceAsync(stream);
                    }
                }
                else
                {
                    // For web URLs
                    bitmap.UriSource = uri;
                }
                
                imageSources.Add(bitmap);
            }

            dispatcherQueue.TryEnqueue(() =>
            {
                Images = imageSources;
            });
        }

        private bool CanExecuteGenerateCommand(object args)
        {
            return !_executingInference;
        }
        
        private int _numberOfImages;
        public int NumberOfImages
        {
            get => _numberOfImages;
            set => SetProperty(ref _numberOfImages, value);
        }

        private string _inputDescription;
        public string InputDescription
        {
            get => _inputDescription;
            set => SetProperty(ref _inputDescription, value);
        }

        private ICollection<ImageSource> _images;
        public ICollection<ImageSource> Images
        {
            get => _images;
            private set => SetProperty(ref _images, value);
        }

        private GeneratorModel? _model;
        public GeneratorModel? Model
        {
            get => _model;
            private set => SetProperty(ref _model, value);
        }

        private double _progressPercentage;
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _statusString;
        public string StatusString
        {
            get => _statusString;
            set => SetProperty(ref _statusString, value);
        }
    }
}
