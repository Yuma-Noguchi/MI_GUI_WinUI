using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.Linq;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ViewModelBase
    {
        private XamlRoot? _xamlRoot;
        private readonly IStableDiffusionService _sdService;
        private bool _executingInference;
        private string[] _currentImagePaths = Array.Empty<string>();
        private string helperPrompt = "{}";
        private readonly StableDiffusionModelManager _modelManager;

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

        public double ProgressPercentage => _sdService?.Percentage ?? 0;

        public bool IsNotGenerating => !IsGenerating;
        public bool IsReady => _sdService.IsInitialized && !IsInitializing;
        public bool CanGenerate => IsReady && !IsGenerating && !string.IsNullOrWhiteSpace(InputDescription);

        public IconStudioViewModel(
            IStableDiffusionService sdService,
            ILogger<IconStudioViewModel> logger,
            INavigationService navigationService)
            : base(logger, navigationService)
        {
            _sdService = sdService ?? throw new ArgumentNullException(nameof(sdService));
            _modelManager = StableDiffusionModelManager.Instance;

            if (_sdService is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(IStableDiffusionService.Percentage))
                    {
                        OnPropertyChanged(nameof(ProgressPercentage));
                    }
                };
            }
        }

        protected override void OnWindowChanged()
        {
            base.OnWindowChanged();
            if (Window != null)
            {
                _xamlRoot = Window.Content?.XamlRoot;
            }
            else
            {
                _xamlRoot = null;
            }
        }

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

        private bool CanGenerateExecute() => !_executingInference;

        private string BuildFinalPrompt(string prompt)
        {
            return helperPrompt.Replace("{}", prompt);
        }

        [RelayCommand(CanExecute = nameof(CanGenerateExecute))]
        private async Task GenerateAsync()
        {
            _executingInference = true;
            IsGenerating = true;

            try
            {
                await ExecuteWithErrorHandlingAsync(async () =>
                {
                    StatusString = "Generating...";

                    if (_sdService.UsingCpuFallback)
                    {
                        StatusMessage = "Running on CPU - generation may take longer";
                    }

                    var prompt = BuildFinalPrompt(InputDescription);
                    _currentImagePaths = await _sdService.GenerateImages(prompt, NumberOfImages);

                    StatusString = "Generation complete";
                    await LoadImagesAsync(_currentImagePaths);
                    IsImageGenerated = true;
                }, nameof(GenerateAsync));
            }
            finally
            {
                _executingInference = false;
                IsGenerating = false;
            }
        }

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
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(IconName))
                {
                    throw new InvalidOperationException("Please enter a name for the icon.");
                }

                if (!Utils.FileNameHelper.IsValidFileName(IconName))
                {
                    throw new InvalidOperationException("The icon name contains invalid characters.");
                }

                if (_currentImagePaths == null || !_currentImagePaths.Any())
                {
                    throw new InvalidOperationException("No image to save.");
                }

                var sanitizedName = Utils.FileNameHelper.SanitizeFileName(IconName);
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MotionInput", "data", "assets", "generated_icons");
                Directory.CreateDirectory(iconPath);
                
                var fileName = Path.Combine(iconPath, $"{sanitizedName}.png");

                if (File.Exists(fileName) && XamlRoot != null)
                {
                    var result = await Utils.DialogHelper.ShowConfirmation(
                        $"An icon named '{sanitizedName}.png' already exists. Do you want to replace it?",
                        "Replace Existing Icon?",
                        XamlRoot);

                    if (!result) return;
                }

                await Utils.ImageHelper.SaveImageAsync(_currentImagePaths.First(), fileName);

                if (XamlRoot != null)
                {
                    await Utils.DialogHelper.ShowMessage($"Icon saved as {sanitizedName}.png", "Success", XamlRoot);
                }
            }, nameof(SaveAsync));
        }

        public async Task InitializeStableDiffusionAsync()
        {
            IsInitializing = true;
            IsPreInitialization = true;
            InitializationFailed = false;
            ErrorMessage = string.Empty;
            InitializationStatus = $"Initializing with {(UseGpu ? "GPU" : "CPU")} acceleration...";

            var wasInitialized = _modelManager.IsInitialized;
            
            try
            {
                await _modelManager.EnsureInitializedAsync(UseGpu);
                await _sdService.Initialize(UseGpu);

                InitializationStatus = "Initialization complete";
                InitializationFailed = false;
                IsPreInitialization = false;

                if (XamlRoot != null)
                {
                    if (!wasInitialized)
                    {
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
                                $"Stable Diffusion initialized successfully with GPU acceleration.",
                                "Initialization Complete",
                                XamlRoot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleInitializationError($"Failed to initialize Icon Studio: {ex.Message}", ex);
            }
            finally
            {
                IsInitializing = false;
            }
        }

        private async Task HandleInitializationError(string message, Exception ex)
        {
            _logger.LogError(ex, message);
            InitializationFailed = true;
            ErrorMessage = message;
            if (XamlRoot != null)
            {
                await Utils.DialogHelper.ShowError(ErrorMessage, XamlRoot);
            }
        }

        [RelayCommand]
        private async Task RetryInitialization()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Retrying initialization");
                InitializationFailed = false;
                ErrorMessage = string.Empty;
                await InitializeStableDiffusionAsync();
            }, nameof(RetryInitialization));
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
                        Directory.Delete(dir, true);
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

        public override void Cleanup()
        {
            try
            {
                // Clear UI resources only
                if (Images is ObservableCollection<ImageSource> collection)
                {
                    collection.Clear();
                }
                Images = null;

                PreviewImage?.Dispose();
                PreviewImage = null;

                _currentImagePaths = Array.Empty<string>();
                StatusMessage = string.Empty;
                InitializationStatus = string.Empty;

                CleanupTempDirectories();
                _xamlRoot = null;

                // Note: Not disposing StableDiffusionService as it's managed by ModelManager
                base.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        protected override async Task ShowErrorAsync(string message)
        {
            ErrorMessage = message;
            if (XamlRoot != null)
            {
                await Utils.DialogHelper.ShowError(message, XamlRoot);
            }
        }
    }
}
