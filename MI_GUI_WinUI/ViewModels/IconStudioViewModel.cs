using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ObservableObject
    {
        private readonly ILogger<IconStudioViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly IStableDiffusionService _sdService;
        private XamlRoot? _xamlRoot;
        private byte[]? _currentImageData;

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
        private int _generationProgress;

        [ObservableProperty]
        private GenerationProgressState _progressState;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isImageGenerated;

        [ObservableProperty]
        private string _iconName = string.Empty;

        public IconStudioViewModel(
            ILogger<IconStudioViewModel> logger,
            INavigationService navigationService,
            IStableDiffusionService sdService)
        {
            _logger = logger;
            _navigationService = navigationService;
            _sdService = sdService;
        }

        [RelayCommand]
        private async Task RetryInitialization()
        {
            _logger.LogInformation("Retrying initialization");
            InitializationFailed = false;
            ErrorMessage = string.Empty;
            await InitializeAsync();
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
                GenerationProgress = 0;
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

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private async Task GenerateAsync()
        {
            try
            {
                IsGenerating = true;
                IsImageGenerated = false;

                var settings = new IconGenerationSettings
                {
                    Prompt = Prompt,
                    Height = 512,
                    Width = 512,
                    NumInferenceSteps = 20,
                    GuidanceScale = 7.5f
                };

                var progress = new Progress<int>(value => {
                    GenerationProgress = value;
                    ProgressState = value switch
                    {
                        < 5 => GenerationProgressState.Loading,
                        < 10 => GenerationProgressState.Tokenizing,
                        < 15 => GenerationProgressState.Encoding,
                        < 20 => GenerationProgressState.InitializingLatents,
                        < 80 => GenerationProgressState.Diffusing,
                        < 90 => GenerationProgressState.Decoding,
                        < 95 => GenerationProgressState.Finalizing,
                        _ => GenerationProgressState.Completing
                    };
                    StatusMessage = GetStatusMessage(ProgressState, value);
                });

                _currentImageData = await _sdService.GenerateImage(settings, progress);
                await UpdatePreviewImage(_currentImageData);
                IsImageGenerated = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image generation");
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("Failed to generate icon. Please try again.", XamlRoot);
                }
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private string GetStatusMessage(GenerationProgressState state, int progress) => state switch
        {
            GenerationProgressState.Loading => "Loading AI models...",
            GenerationProgressState.Tokenizing => "Tokenizing prompt...",
            GenerationProgressState.Encoding => "Processing text embeddings...",
            GenerationProgressState.InitializingLatents => "Initializing latent space...",
            GenerationProgressState.Diffusing => $"Running diffusion step {(progress - 20) * 100 / 60:0}%...",
            GenerationProgressState.Decoding => "Decoding image...",
            GenerationProgressState.Finalizing => "Applying final touches...",
            _ => "Preparing output..."
        };

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

                if (_currentImageData == null)
                {
                    if (XamlRoot != null)
                    {
                        await Utils.DialogHelper.ShowError("No image data available to save.", XamlRoot);
                    }
                    return;
                }
                
                await File.WriteAllBytesAsync(fileName, _currentImageData);

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

        private async Task UpdatePreviewImage(byte[] imageData)
        {
            try
            {
                PreviewImage = await Utils.ImageHelper.ByteArrayToImageSource(imageData);
                if (PreviewImage == null)
                {
                    _logger.LogError("Failed to convert image data to preview");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preview image");
                throw;
            }
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
                        $"Icon Studio initialized successfully with {(UseGpu ? "GPU" : "CPU")} acceleration.", 
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

        

        public void Cleanup()
        {
            _currentImageData = null;
            PreviewImage = null;
            GenerationProgress = 0;
            StatusMessage = string.Empty;
            ProgressState = GenerationProgressState.Loading;
            InitializationStatus = string.Empty;
            GenerationProgress = 0;
            StatusMessage = string.Empty;
            ProgressState = GenerationProgressState.Loading;
            InitializationStatus = string.Empty;
        }
    }
}
