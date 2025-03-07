using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using StableDiffusion.ML.OnnxRuntime;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace MI_GUI_WinUI.Services
{
    public partial class StableDiffusionService : ObservableObject, IDisposable
    {
        private StableDiffusionConfig _config;
        private UNet _unet;
        private readonly ILogger<StableDiffusionService> _logger;
        private readonly object _initLock = new();
        private readonly DispatcherQueue _dispatcherQueue;

        public bool IsInitialized => _unet != null && _config != null;

        [ObservableProperty]
        private double _percentage;

        [ObservableProperty]
        private long _lastTimeMilliseconds;

        [ObservableProperty]
        private double _iterationsPerSecond;

        public long NumInferenceSteps => _config.NumInferenceSteps;

        public StableDiffusionService(ILogger<StableDiffusionService> logger)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            var modelsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Onnx", "fp16");

            _config = new StableDiffusionConfig
            {
                NumInferenceSteps = 50,
                GuidanceScale = 7.5,
                ExecutionProviderTarget = StableDiffusionConfig.ExecutionProvider.DirectML,
                DeviceId = 1,
                TokenizerOnnxPath = $@"{modelsPath}\cliptokenizer.onnx",
                TextEncoderOnnxPath = $@"{modelsPath}\text_encoder\model.onnx",
                UnetOnnxPath = $@"{modelsPath}\unet\model.onnx",
                VaeDecoderOnnxPath = $@"{modelsPath}\vae_decoder\model.onnx",
                SafetyModelPath = $@"{modelsPath}\safety_checker\model.onnx",
                ImageOutputPath = "NONE",
            };

            _logger = logger;
        }

        public async Task Initialize(bool useGpu)
        {
            // Ensure we're not already initialized
            if (IsInitialized)
            {
                _logger.LogInformation("Service already initialized");
                return;
            }

            // Use lock to prevent multiple simultaneous initialization attempts
            lock (_initLock)
            {
                if (IsInitialized)
                    return;
            }

            try
            {
                _logger.LogInformation("Starting initialization with GPU: {useGpu}", useGpu);
                await Task.Run(() => {
                    _unet = new UNet(_config);
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Required model file not found: {file}", ex.FileName);
                CleanupAfterFailure();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Stable Diffusion service");
                CleanupAfterFailure();
                throw;
            }
        }

        private void CleanupAfterFailure()
        {
            lock (_initLock)
            {
                _unet = null;
                _config = null;
            }
        }

        public ObservableCollection<ImageSource> GenerateFakeData(string description, int numberOfImages)
        {
            string imagePath = "pack://application:,,,/Assets/biaafpwi.png";
            var tempImages = new ObservableCollection<ImageSource>();
            for (int i = 0; i < numberOfImages; i++)
            {
                var bitmapImage = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                tempImages.Add(bitmapImage);
            }

            return tempImages;
        }

        public async Task<string[]> GenerateImages(string description, int numberOfImages, Action<int>? stepCallback = null)
        {
            _dispatcherQueue.TryEnqueue(() => Percentage = 0);
            
            if (numberOfImages == 0)
            {
                return Array.Empty<string>();
            }

            var result = await Task.Run(() =>
            {
                var timer = new Stopwatch();
                timer.Start();

                var imageDestination = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Images-{DateTime.Now.Ticks}");
                var config = _config;
                config.ImageOutputPath = imageDestination;
                var totalSteps = numberOfImages * config.NumInferenceSteps;

                Directory.CreateDirectory(imageDestination);

                for (int i = 1; i <= numberOfImages; i++) 
                {
                    var output = _unet.Inference(
                        description, 
                        config, 
                        (stepIndex) => 
                        { 
                            var percentage = ((double)((stepIndex + 1) + (i - 1) * config.NumInferenceSteps) / (double)totalSteps) * 100.0;
                            _dispatcherQueue.TryEnqueue(() => Percentage = percentage);
                        }
                    );

                    _dispatcherQueue.TryEnqueue(() => IterationsPerSecond = output.IterationsPerSecond);

                    if (output.Image == null)
                    {
                        _logger.LogWarning("Error generating image {i}", i);
                    }
                }

                var imagePaths = Directory.GetFiles(imageDestination, "*.*", SearchOption.AllDirectories)
                                        .Where(path => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }
                                        .Contains(Path.GetExtension(path).ToLower()))
                                        .ToArray();

                timer.Stop();
                _dispatcherQueue.TryEnqueue(() => LastTimeMilliseconds = timer.ElapsedMilliseconds);

                return imagePaths;
            });

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_initLock)
                {
                    _unet?.Dispose();
                    _unet = null;
                    _config = null;
                }
            }
        }
    }
}
