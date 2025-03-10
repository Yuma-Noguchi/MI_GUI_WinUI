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
using System.Linq;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ObservableObject
    {
        private XamlRoot? _xamlRoot;
        private readonly ILogger<IconStudioViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly StableDiffusionService _sdService;
        private bool _executingInference;
        private string[] _currentImagePaths = Array.Empty<string>();

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
        private string _initializationStatus = "Starting initialization...";

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
        private string _inputDescription = "";

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
            if (_sdService.IsInitialized)
            {
                IsPreInitialization = false;
                return;
            }

            try
            {
                IsInitializing = true;
                IsPreInitialization = true;
                InitializationFailed = false;
                ErrorMessage = string.Empty;

                InitializationStatus = $"Initializing with {(UseGpu ? "GPU" : "CPU")} acceleration...";
                await _sdService.Initialize(UseGpu);

                InitializationStatus = "Initialization complete";
                InitializationFailed = false;
                IsPreInitialization = false;

                if (XamlRoot != null)
                {
                    // Check if we fell back to CPU
                    if (UseGpu && _sdService.UsingCpuFallback)
                    {
                        await Utils.DialogHelper.ShowMessage(
                            "GPU acceleration was not available or failed to initialize.\n\n" +
                            "The application will run using CPU instead, which may be significantly slower.",
                            "Using CPU Mode",
                            XamlRoot);
                    }
                    else
                    {
                        await Utils.DialogHelper.ShowMessage(
                            $"Stable Diffusion initialized successfully with {(_sdService.UsingCpuFallback ? "CPU" : "GPU")} acceleration.",
                            "Initialization Complete",
                            XamlRoot);
                    }
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
                _logger.LogError(ex, "Failed to initialize Icon Studio");
                InitializationFailed = true;
                ErrorMessage = $"Failed to initialize Icon Studio with {(UseGpu ? "GPU" : "CPU")}. {ex.Message}";
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
        public bool CanGenerate => IsReady && !IsGenerating && !string.IsNullOrWhiteSpace(InputDescription);

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

        partial void OnInputDescriptionChanged(string value)
        {
            OnPropertyChanged(nameof(CanGenerate));
        }

        //private string helperPrompt = "minimalist clean icon representing {}, circular button design, game controller style, flat vector art, centered composition, solid background, accessibility-focused, glowing edges, neon accen";
        //private string helperPrompt = "single clean vector icon, representing {}, modern UI style, simple, flat design, bright colors, no background, isolated icon";
        //private string helperPrompt = "single clean vector icon representing {}, modern material design, minimal UI/UX style, perfect pixel grid alignment, scalable vector graphics, professional app icon quality, crisp edges";
        private string helperPrompt = "{}";

        private string BuildFinalPrompt(string prompt)
        {
            return helperPrompt.Replace("{}", prompt);
        }

        [RelayCommand(CanExecute = nameof(CanGenerateExecute))]
        private async Task GenerateAsync()
        {
            try
            {
                _executingInference = true;
                StatusString = "Generating...";
                IsGenerating = true;

                // If we're using CPU, warn the user about slower performance
                if (_sdService.UsingCpuFallback)
                {
                    StatusMessage = "Running on CPU - generation may take longer";
                }

                var prompt = BuildFinalPrompt(InputDescription);

                _currentImagePaths = await _sdService.GenerateImages(prompt, NumberOfImages);

                StatusString = $"{_sdService.NumInferenceSteps} iterations ({_sdService.IterationsPerSecond:F1} it/sec); {_sdService.LastTimeMilliseconds / 1000.0:F1} sec total";
                await LoadImagesAsync(_currentImagePaths);
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

            if (_currentImagePaths == null || !_currentImagePaths.Any())
            {
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("No image to save.", XamlRoot);
                }
                return;
            }

            try
            {
                var sanitizedName = Utils.FileNameHelper.SanitizeFileName(IconName);
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionInput", "data", "assets", "generated_icons");
                Directory.CreateDirectory(iconPath);

                var fileName = Path.Combine(iconPath, $"{sanitizedName}.png");

                // Check if file exists
                if (File.Exists(fileName))
                {
                    var overwrite = await Utils.DialogHelper.ShowConfirmation(
                        $"An icon named '{sanitizedName}.png' already exists.\nDo you want to replace it?",
                        "Replace Existing Icon?",
                        XamlRoot);

                    // If user chooses not to replace, return to page
                    if (!overwrite)
                    {
                        return;
                    }
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                // Save the first generated image
                await Utils.ImageHelper.SaveImageAsync(_currentImagePaths.First(), fileName);

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

        private void CleanupTempDirectories()
        {
            try
            {
                var appPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                var tempDirs = Directory.GetDirectories(appPath, "Images-*");
                
                foreach (var dir in tempDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);  // recursive delete
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete temp directory: {dir}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temp directories");
            }
        }

        public void Cleanup()
        {
            Images = null;
            PreviewImage = null;
            StatusMessage = string.Empty;
            InitializationStatus = string.Empty;
            _currentImagePaths = Array.Empty<string>();
            CleanupTempDirectories();
        }
    }
}
