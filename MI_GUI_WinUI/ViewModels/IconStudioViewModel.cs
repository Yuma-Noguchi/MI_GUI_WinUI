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
using Microsoft.UI.Xaml;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ObservableObject
    {
        private XamlRoot? _xamlRoot;
        private readonly ILogger<IconStudioViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly StableDiffusionService _sdService;
        private bool _executingInference;

        public XamlRoot? XamlRoot
        {
            get => _xamlRoot;
            set => _xamlRoot = value;
        }

        [ObservableProperty]
        private string _title = "Icon Studio";

        [ObservableProperty]
        private bool _isInitializing;

        [ObservableProperty]
        private bool _isPreInitialization = true;

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

        public double ProgressPercentage => _sdService.Percentage;

        public IconStudioViewModel(
            StableDiffusionService sdService,
            ILogger<IconStudioViewModel> logger,
            INavigationService navigationService)
        {
            _sdService = sdService;
            _logger = logger;
            _navigationService = navigationService;

            // Subscribe to service's percentage changes
            _sdService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StableDiffusionService.Percentage))
                {
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            };
        }

        public async Task InitializeAsync()
        {
            InitializationFailed = false;
            ErrorMessage = string.Empty;

            if (_sdService.IsInitialized || IsInitializing)
                return;

            try
            {
                IsInitializing = true;
                InitializationStatus = "Starting initialization...";

                InitializationStatus = $"Initializing with {(UseGpu ? "GPU" : "CPU")} acceleration...";
                await _sdService.Initialize(!UseGpu);

                InitializationStatus = "Initialization complete";
                InitializationFailed = false;
                IsPreInitialization = false;

                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowMessage(
                        $"Stable Diffusion initialized successfully with {(UseGpu ? "GPU" : "CPU")} acceleration.",
                        "Initialization Complete",
                        XamlRoot);
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "AI Models directory not found");
                InitializationFailed = true;
                ErrorMessage = "AI Models directory not found. Please ensure the models are properly installed.";
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError(ErrorMessage, XamlRoot);
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Required model file not found");
                InitializationFailed = true;
                ErrorMessage = $"Required model file not found: {Path.GetFileName(ex.FileName)}. Please ensure all model files are present.";
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError(ErrorMessage, XamlRoot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Stable Diffusion");
                InitializationFailed = true;
                ErrorMessage = $"Failed to initialize Stable Diffusion with {(UseGpu ? "GPU" : "CPU")}. {ex.Message}";
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError(ErrorMessage, XamlRoot);
                }
            }
            finally
            {
                IsInitializing = false;
            }
        }

        public bool IsNotGenerating => !IsGenerating;
        public bool IsReady => _sdService.IsInitialized && !IsInitializing;
        public bool CanGenerate => IsReady && !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);

        partial void OnIsGeneratingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotGenerating));
            OnPropertyChanged(nameof(CanGenerate));
            if (!value)
            {
                StatusMessage = string.Empty;
            }
        }

        partial void OnIsInitializingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(CanGenerate));
        }

        partial void OnPromptChanged(string value)
        {
            OnPropertyChanged(nameof(CanGenerate));
        }

        [RelayCommand(CanExecute = nameof(CanGenerateExecute))]
        private async Task GenerateAsync()
        {
            try
            {
                _executingInference = true;
                StatusString = "Generating...";
                IsGenerating = true;

                var imagePaths = await _sdService.GenerateImages(InputDescription, NumberOfImages);

                StatusString = $"{_sdService.NumInferenceSteps} iterations ({_sdService.IterationsPerSecond:F1} it/sec); {_sdService.LastTimeMilliseconds / 1000.0:F1} sec total";
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
            if (string.IsNullOrWhiteSpace(IconName))
            {
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("Please enter a name for the icon.", XamlRoot);
                }
                return;
            }

            if (!Utils.FileNameHelper.IsValidFileName(IconName))
            {
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("The icon name contains invalid characters. Please use only letters, numbers, and basic punctuation.", XamlRoot);
                }
                return;
            }

            try
            {
                var sanitizedName = Utils.FileNameHelper.SanitizeFileName(IconName);
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionInput", "data", "assets", "generated_icons");
                Directory.CreateDirectory(iconPath);

                var fileName = Path.Combine(iconPath, $"{sanitizedName}.png");

                if (File.Exists(fileName))
                {
                    if (XamlRoot != null)
                    {
                        var overwrite = await Utils.DialogHelper.ShowConfirmation(
                            $"An icon named '{sanitizedName}.png' already exists. Do you want to replace it?",
                            "Icon Already Exists",
                            XamlRoot);

                        if (!overwrite)
                            return;
                    }
                }

                //if (_currentImageData == null)
                //{
                //    if (XamlRoot != null)
                //    {
                //        await Utils.DialogHelper.ShowError("No image data available to save.", XamlRoot);
                //    }
                //    return;
                //}

                //await File.WriteAllBytesAsync(fileName, _currentImageData);

                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowMessage($"Icon saved as {sanitizedName}.png", "Success", XamlRoot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving icon");
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("Failed to save icon. Please try again.", XamlRoot);
                }
            }
        }

        [RelayCommand]
        private async Task RetryInitialization()
        {
            _logger.LogInformation("Retrying initialization");
            InitializationFailed = false;
            ErrorMessage = string.Empty;
            await InitializeAsync();
        }

        public void Cleanup()
        {
            Images = null;
            PreviewImage = null;
            StatusMessage = string.Empty;
            InitializationStatus = string.Empty;
        }
    }
}
