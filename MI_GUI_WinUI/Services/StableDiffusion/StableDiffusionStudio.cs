using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public class StableDiffusionStudio : IDisposable
    {
        private readonly StableDiffusionConfig _config;
        private readonly ILogger<StableDiffusionStudio> _logger;
        private readonly TextProcessor _textProcessor;
        private readonly UNet _unet;
        private readonly VaeDecoder _vaeDecoder;
        private bool _disposedValue;

        public StableDiffusionStudio(StableDiffusionConfig config, ILoggerFactory loggerFactory)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<StableDiffusionStudio>();
            _textProcessor = new TextProcessor(config, loggerFactory.CreateLogger<TextProcessor>());
            _unet = new UNet(config, loggerFactory.CreateLogger<UNet>());
            _vaeDecoder = new VaeDecoder(config, loggerFactory.CreateLogger<VaeDecoder>());
        }

        public async Task<Image<Rgba32>> GenerateImageAsync(string prompt, IProgress<double> progress, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting image generation with prompt: {prompt}", prompt);

                // Process text
                var textEmbeddings = _textProcessor.ProcessText(prompt);
                if (textEmbeddings is not DenseTensor<float> denseEmbeddings)
                {
                    throw new InvalidOperationException("Text embeddings must be DenseTensor<float>");
                }
                LogTensorShape("Text embeddings", denseEmbeddings);

                // Initialize scheduler
                var scheduler = new LMSDiscreteScheduler(_config);
                var timesteps = scheduler.SetTimesteps(_config.NumInferenceSteps);
                _logger.LogDebug("Scheduler initialized with {count} timesteps", timesteps.Length);

                // Generate initial noise
                var latents = GenerateLatentSample(new Random().Next(), scheduler.InitNoiseSigma);
                LogTensorShape("Initial latents", latents);
                
                // Denoising loop
                for (int t = 0; t < timesteps.Length; t++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Generation cancelled at step {step}", t);
                        throw new OperationCanceledException();
                    }

                    var latentModelInput = TensorHelper.Duplicate(latents.ToArray(), new[] { 2, 4, _config.Height / 8, _config.Width / 8 });

                    var scaledLatents = scheduler.ScaleInput(latents.ToArray(), timesteps[t]);

                    // UNet step
                    var noisePred = _unet.Predict(denseEmbeddings, scaledLatents, timesteps[t]);
                    LogTensorShape("Noise prediction", noisePred);

                    // Scheduler step with default order (4)
                    latents = scheduler.Step(noisePred, timesteps[t], latents);
                    LogTensorShape("Updated latents", latents);

                    // Report progress
                    var progressValue = (double)(t + 1) / timesteps.Length;
                    progress?.Report(progressValue);
                }

                // Decode image
                _logger.LogDebug("Decoding final image from latents");
                var image = _vaeDecoder.Decode(latents);

                _logger.LogInformation("Image generation completed successfully");
                return image;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during image generation");
                throw;
            }
        }

        private DenseTensor<float> GenerateLatentSample(int seed, float initNoiseSigma)
        {
            var random = new Random(seed);
            var batchSize = 1;
            var channels = 4;
            var height = _config.Height / 8;
            var width = _config.Width / 8;
            var dimensions = new[] { batchSize, channels, height, width };

            _logger.LogDebug("Generating latent sample with dimensions {dimensions}", 
                string.Join("x", dimensions));

            // Fill with random values using Box-Muller transform
            var totalSize = TensorHelper.GetTotalSize(dimensions);
            var latentsArray = new float[totalSize];

            for (int i = 0; i < totalSize; i += 2)
            {
                var u1 = random.NextDouble();
                var u2 = random.NextDouble();
                var radius = Math.Sqrt(-2.0 * Math.Log(u1));
                var theta = 2.0 * Math.PI * u2;

                // Generate two normally distributed numbers
                latentsArray[i] = (float)(radius * Math.Cos(theta)) * initNoiseSigma;
                if (i + 1 < totalSize)
                {
                    latentsArray[i + 1] = (float)(radius * Math.Sin(theta)) * initNoiseSigma;
                }
            }

            return TensorHelper.CreateTensor(latentsArray, dimensions);
        }

        private void LogTensorShape(string name, DenseTensor<float> tensor)
        {
            if (tensor == null)
            {
                _logger.LogWarning($"{name} tensor is null");
                return;
            }

            var dimensions = tensor.Dimensions.ToArray();
            var shape = string.Join("x", dimensions.Select(d => d.ToString()));
            _logger.LogDebug($"{name} shape: {shape}");
        }

        public virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _textProcessor?.Dispose();
                    _unet?.Dispose();
                    _vaeDecoder?.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
