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

        public bool IsNotGenerating => !IsGenerating;

        partial void OnIsGeneratingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotGenerating));
            if (!value)
            {
                GenerationProgress = 0;
            }
        }

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
        private async Task GenerateAsync()
        {
            if (!_sdService.IsInitialized)
            {
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("Icon Studio is not initialized. Please try restarting the application.", XamlRoot);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(Prompt))
            {
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError("Please enter a prompt for icon generation.", XamlRoot);
                }
                return;
            }

            try
            {
                IsGenerating = true;
                var settings = new IconGenerationSettings
                {
                    Prompt = Prompt,
                    ImageSize = new System.Drawing.Size(60, 60),
                    NumInferenceSteps = 20,
                    GuidanceScale = 7.5f,
                    Seed = new Random().Next()
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
                if (PreviewImage == null)
                {
                    if (XamlRoot != null)
                    {
                        await Utils.DialogHelper.ShowError("No image available to save.", XamlRoot);
                    }
                    return;
                }

                // Save the current image
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
            try
            {
                if (!_sdService.IsInitialized)
                {
                    await _sdService.Initialize(UseGpu);
                    if (XamlRoot != null)
                    {
                        await Utils.DialogHelper.ShowMessage(
                            $"Icon Studio initialized successfully with {(UseGpu ? "GPU" : "CPU")} acceleration.", 
                            "Initialization Complete", 
                            XamlRoot);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Icon Studio");
                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowError(
                        $"Failed to initialize Icon Studio with {(UseGpu ? "GPU" : "CPU")}. Please check if ONNX model files are present in the AI_Models directory.",
                        XamlRoot);
                }
            }
        }

        public void Cleanup()
        {
            _currentImageData = null;
            PreviewImage = null;
            GenerationProgress = 0;
            StatusMessage = string.Empty;
            ProgressState = GenerationProgressState.Loading;
        }
    }
}
