using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StableDiffusion.ML.OnnxRuntime;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Singleton manager for Stable Diffusion model lifecycle and persistence
    /// </summary>
    public class StableDiffusionModelManager : IDisposable
    {
        private static readonly Lazy<StableDiffusionModelManager> _instance = 
            new(() => new StableDiffusionModelManager());

        private bool _isInitialized;
        private bool _isInitializing;
        private int _referenceCount;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly ILogger<StableDiffusionModelManager> _logger;
        private bool _disposed;
        private UNet? _unet;
        private StableDiffusionConfig? _config;
        private string _modelsPath;

        /// <summary>
        /// Gets the singleton instance of the model manager
        /// </summary>
        public static StableDiffusionModelManager Instance => _instance.Value;

        /// <summary>
        /// Gets the current UNet model instance
        /// </summary>
        public UNet? UNet => _unet;

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public StableDiffusionConfig? Config => _config;

        /// <summary>
        /// Gets whether the models are initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets whether GPU acceleration is being used
        /// </summary>
        public bool UsingGpu { get; private set; }

        private StableDiffusionModelManager()
        {
            _logger = App.Current.Services.GetRequiredService<ILogger<StableDiffusionModelManager>>();
            var basePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            _modelsPath = Path.Combine(basePath, "Onnx", "fp16");
        }

        /// <summary>
        /// Ensures the models are initialized, managing the initialization lifecycle
        /// </summary>
        /// <param name="useGpu">Whether to use GPU acceleration</param>
        public async Task EnsureInitializedAsync(bool useGpu)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(StableDiffusionModelManager));
            }

            await _initLock.WaitAsync();
            try
            {
                if (!_isInitialized && !_isInitializing)
                {
                    _isInitializing = true;
                    _logger?.LogInformation($"Initializing models with GPU: {useGpu}");
                    
                    await InitializeModelsAsync(useGpu);
                    
                    _isInitialized = true;
                    UsingGpu = useGpu;
                    _logger?.LogInformation("Model initialization complete");
                }
                else if (_isInitialized && UsingGpu != useGpu)
                {
                    _logger?.LogWarning($"Models already initialized with GPU: {UsingGpu}. Requested GPU: {useGpu}");
                }

                _referenceCount++;
                _logger?.LogDebug($"Reference count increased to: {_referenceCount}");
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                _isInitialized = false;
                _logger?.LogError(ex, "Failed to initialize models");
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Releases a reference to the models, potentially triggering cleanup
        /// </summary>
        public void ReleaseReference()
        {
            if (_disposed) return;

            int newCount = Interlocked.Decrement(ref _referenceCount);
            _logger?.LogDebug($"Reference count decreased to: {newCount}");

            if (newCount <= 0)
            {
                // Optional: Implement delayed cleanup here
                _logger?.LogInformation("Reference count reached zero");
            }
        }

        private async Task InitializeModelsAsync(bool useGpu)
        {
            try
            {
                _config = new StableDiffusionConfig
                {
                    NumInferenceSteps = 75,
                    GuidanceScale = 8.5,
                    ExecutionProviderTarget = useGpu
                        ? StableDiffusionConfig.ExecutionProvider.DirectML
                        : StableDiffusionConfig.ExecutionProvider.Cpu,
                    DeviceId = 1,
                    TokenizerOnnxPath = $@"{_modelsPath}\cliptokenizer.onnx",
                    TextEncoderOnnxPath = $@"{_modelsPath}\text_encoder\model.onnx",
                    UnetOnnxPath = $@"{_modelsPath}\unet\model.onnx",
                    VaeDecoderOnnxPath = $@"{_modelsPath}\vae_decoder\model.onnx",
                    SafetyModelPath = $@"{_modelsPath}\safety_checker\model.onnx",
                    ImageOutputPath = "NONE",
                };

                _logger?.LogInformation("Starting initialization with execution provider: {provider}",
                    _config.ExecutionProviderTarget);

                try
                {
                    await Task.Run(() => {
                        _unet = new UNet(_config);
                    });
                    UsingGpu = useGpu;
                }
                catch (Exception ex) when (useGpu &&
                    (ex.Message.Contains("DirectML") ||
                     ex.Message.Contains("GPU") ||
                     ex.Message.Contains("DML") ||
                     ex.GetType().Name.Contains("Dml")))
                {
                    _logger?.LogWarning(ex, "DirectML initialization failed, falling back to CPU");

                    _config.ExecutionProviderTarget = StableDiffusionConfig.ExecutionProvider.Cpu;
                    await Task.Run(() => {
                        _unet = new UNet(_config);
                    });
                    UsingGpu = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing models");
                _unet?.Dispose();
                _unet = null;
                _config = null;
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _initLock.Dispose();
                    _unet?.Dispose();
                    _unet = null;
                    _config = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}