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
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Dispatching;
using MI_GUI_WinUI.Services.Interfaces;

namespace MI_GUI_WinUI.Services
{
    public partial class StableDiffusionService : ObservableObject, IStableDiffusionService
    {
        private readonly ILogger<StableDiffusionService> _logger;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly StableDiffusionModelManager _modelManager;
        private readonly string _outputPath;

        public bool IsInitialized => _modelManager.IsInitialized;
        public bool UsingCpuFallback => !_modelManager.UsingGpu;
        public long NumInferenceSteps => 75; // Default value

        [ObservableProperty]
        private double _percentage;

        [ObservableProperty]
        private long _lastTimeMilliseconds;

        [ObservableProperty]
        private double _iterationsPerSecond;

        public StableDiffusionService(ILogger<StableDiffusionService> logger)
        {
            _logger = logger;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _modelManager = StableDiffusionModelManager.Instance;
            _outputPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        }

        public async Task Initialize(bool useGpu)
        {
            try
            {
                await _modelManager.EnsureInitializedAsync(useGpu);
                _logger.LogInformation("Service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Stable Diffusion service");
                throw;
            }
        }

        public ObservableCollection<ImageSource> GenerateFakeData(string description, int numberOfImages)
        {
            _logger.LogInformation("Generating fake data: {count} images with description: {desc}",
                numberOfImages, description);

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
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Service not initialized");
            }

            _dispatcherQueue.TryEnqueue(() => Percentage = 0);
            
            if (numberOfImages == 0)
            {
                _logger.LogWarning("Requested to generate 0 images");
                return Array.Empty<string>();
            }

            _logger.LogInformation("Starting image generation: {count} images with description: {desc}",
                numberOfImages, description);

            var result = await Task.Run(() =>
            {
                var timer = new Stopwatch();
                timer.Start();

                var imageDestination = Path.Combine(_outputPath, $"Images-{DateTime.Now.Ticks}");
                Directory.CreateDirectory(imageDestination);

                _modelManager.Config.ImageOutputPath = imageDestination;
                var totalSteps = numberOfImages * NumInferenceSteps;
                var generatedPaths = new List<string>();

                for (int i = 1; i <= numberOfImages; i++)
                {
                    _logger.LogDebug("Generating image {current} of {total}", i, numberOfImages);

                    var output = _modelManager.UNet.Inference(
                        description,
                        _modelManager.Config,
                        (stepIndex) =>
                        {
                            var percentage = ((double)((stepIndex + 1) + (i - 1) * NumInferenceSteps) / (double)totalSteps) * 100.0;
                            _dispatcherQueue.TryEnqueue(() => Percentage = percentage);
                            stepCallback?.Invoke(stepIndex);
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

                _logger.LogInformation("Generated {count} images in {time}ms",
                    imagePaths.Length, timer.ElapsedMilliseconds);

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
                _logger.LogInformation("Disposing Stable Diffusion service");
                _modelManager.ReleaseReference();
            }
        }
    }
}