using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MI_GUI_WinUI.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MI_GUI_WinUI.ViewModels
{
    public partial class IconStudioViewModel : ObservableObject
    {
        private readonly ILogger<IconStudioViewModel> _logger;
        private readonly StableDiffusionService _stableDiffusionService;
        private bool _isInitialized;

        [ObservableProperty]
        private string title = "Icon Studio";

        [ObservableProperty]
        private string prompt = string.Empty;

        [ObservableProperty]
        private ImageSource? previewImage;

        [ObservableProperty]
        private bool isGenerating;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool hasPreview;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        
        public bool CanGenerate => !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);

        public IconStudioViewModel(
            ILogger<IconStudioViewModel> logger,
            StableDiffusionService stableDiffusionService)
        {
            _logger = logger;
            _stableDiffusionService = stableDiffusionService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsGenerating = false;
                ErrorMessage = null;
                HasPreview = false;
                PreviewImage = null;

                // Initialize the Stable Diffusion service
                await _stableDiffusionService.InitializeAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing IconStudioViewModel");
                ErrorMessage = "Failed to initialize. Please try again.";
                _isInitialized = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private async Task GenerateIconAsync()
        {
            try
            {
                IsGenerating = true;
                ErrorMessage = null;

                var settings = new StableDiffusionService.GenerationSettings
                {
                    Width = 512,
                    Height = 512,
                    GuidanceScale = 7.5f,
                    Steps = 50
                };

                var imageData = await _stableDiffusionService.GenerateIconAsync(Prompt, settings);
                
                // Save the generated image to a temporary file
                var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                    "GeneratedIcons",
                    CreationCollisionOption.OpenIfExists);
                
                var file = await folder.CreateFileAsync(
                    $"icon_{DateTime.Now:yyyyMMddHHmmss}.png",
                    CreationCollisionOption.GenerateUniqueName);

                await FileIO.WriteBytesAsync(file, imageData);

                // Load the image into the preview
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                PreviewImage = bitmap;
                HasPreview = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating icon");
                ErrorMessage = "Failed to generate icon. Please try again.";
                HasPreview = false;
                PreviewImage = null;
            }
            finally
            {
                IsGenerating = false;
            }
        }

        [RelayCommand]
        private async Task SaveIconAsync()
        {
            if (!HasPreview) return;

            try
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    SuggestedFileName = $"icon_{DateTime.Now:yyyyMMddHHmmss}",
                    FileTypeChoices = { { "PNG Image", new List<string>() { ".png" } } }
                };

                // Get the window handle for the picker
                var window = Application.Current as App;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window.Services.GetRequiredService<WindowManager>().MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    // Copy from temp file to selected location
                    var tempFolder = await ApplicationData.Current.TemporaryFolder.GetFolderAsync("GeneratedIcons");
                    var tempFiles = await tempFolder.GetFilesAsync();
                    if (tempFiles.Count > 0)
                    {
                        await tempFiles[tempFiles.Count - 1].CopyAndReplaceAsync(file);
                        ErrorMessage = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving icon");
                ErrorMessage = "Failed to save icon. Please try again.";
            }
        }

        [RelayCommand]
        private async Task RegenerateIconAsync()
        {
            if (HasPreview)
            {
                await GenerateIconAsync();
            }
        }

        [RelayCommand]
        private void DiscardIcon()
        {
            HasPreview = false;
            PreviewImage = null;
            ErrorMessage = null;
        }

        public void Cleanup()
        {
            PreviewImage = null;
            HasPreview = false;
            _isInitialized = false;
        }
    }
}