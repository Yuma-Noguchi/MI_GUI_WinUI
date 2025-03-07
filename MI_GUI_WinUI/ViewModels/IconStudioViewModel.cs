using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using MI_GUI_WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ObservableObject
    {
        private readonly ILogger<IconStudioViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly StableDiffusionService _service;
        private bool _executingInference;

        [ObservableProperty]
        private string _title = "Icon Studio";

        [ObservableProperty]
        private bool _isInitializing;

        [ObservableProperty]
        private bool _isPreInitialization;

        [ObservableProperty]
        private string _initializationStatus = string.Empty;

        [ObservableProperty]
        private bool _initializationFailed;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _prompt = string.Empty;

        [ObservableProperty]
        private bool _useGpu = true;

        [ObservableProperty]
        private SoftwareBitmapSource? _previewImage;

        [ObservableProperty]
        private bool _isGenerating;

        [ObservableProperty]
        private string _statusString = "Idle";

        [ObservableProperty]
        private bool _isImageGenerated;

        [ObservableProperty]
        private string _iconName = string.Empty;

        [ObservableProperty]
        private int _numberOfImages = 1;

        [ObservableProperty]
        private string _inputDescription = "landscape, painting, rolling hills, windmill, clouds";

        [ObservableProperty]
        private ICollection<ImageSource> _images = new ObservableCollection<ImageSource>();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public IconStudioViewModel(
            StableDiffusionService service,
            ILogger<IconStudioViewModel> logger,
            INavigationService navigationService)
        {
            _service = service;
            _logger = logger;
            _navigationService = navigationService;
        }

        public bool IsNotGenerating => !IsGenerating;
        public bool IsReady => true;
        public bool CanGenerate => IsReady && !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);

        [RelayCommand(CanExecute = nameof(CanGenerateExecute))]
        private async Task GenerateAsync()
        {
            try
            {
                _executingInference = true;
                StatusString = "Generating...";
                IsGenerating = true;

                var imagePaths = await _service.GenerateImages(InputDescription, NumberOfImages);

                StatusString = $"{_service.NumInferenceSteps} iterations ({_service.IterationsPerSecond:F1} it/sec); {_service.LastTimeMilliseconds / 1000.0:F1} sec total";
                await LoadImagesAsync(imagePaths);
                IsImageGenerated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating images");
                StatusString = "Error generating images";
            }
            finally
            {
                _executingInference = false;
                IsGenerating = false;
            }
        }

        private bool CanGenerateExecute() => !_executingInference;

        private async Task LoadImagesAsync(IEnumerable<string> imagePaths)
        {
            var imageSources = new ObservableCollection<ImageSource>();

            foreach (string imagePath in imagePaths)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    var uri = new Uri(imagePath);

                    if (uri.Scheme == "file")
                    {
                        var file = await StorageFile.GetFileFromPathAsync(imagePath);
                        using (var stream = await file.OpenReadAsync())
                        {
                            await bitmap.SetSourceAsync(stream);
                        }
                    }
                    else
                    {
                        bitmap.UriSource = uri;
                    }

                    imageSources.Add(bitmap);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading image: {imagePath}");
                }
            }

            Images = imageSources;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            // TODO: Implement save functionality when needed
        }

        [RelayCommand]
        private async Task RetryInitialization()
        {

        }
    }
}
