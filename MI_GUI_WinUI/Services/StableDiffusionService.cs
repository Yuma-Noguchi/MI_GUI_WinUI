using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MI_GUI_WinUI.Services
{
    public class StableDiffusionService : IDisposable
    {
        private readonly ILogger<StableDiffusionService> _logger;
        private InferenceSession? _session;
        private bool _disposed;

        public StableDiffusionService(ILogger<StableDiffusionService> logger)
        {
            _logger = logger;
        }

        public struct GenerationSettings
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public float GuidanceScale { get; set; }
            public int Steps { get; set; }

            public static GenerationSettings Default => new GenerationSettings
            {
                Width = 512,
                Height = 512,
                GuidanceScale = 7.5f,
                Steps = 50
            };
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Create session options
                var options = new SessionOptions();
                options.AppendExecutionProvider_DML(0); // Use default GPU
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

                // Load model
                var modelPath = Path.Combine(
                    ApplicationData.Current.LocalFolder.Path,
                    "Models",
                    "stable-diffusion.onnx");

                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException("ONNX model file not found", modelPath);
                }

                _session = new InferenceSession(modelPath, options);
                _logger.LogInformation("Successfully initialized Stable Diffusion model");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Stable Diffusion model");
                throw;
            }
        }

        public async Task<byte[]> GenerateIconAsync(string prompt, GenerationSettings settings)
        {
            if (_session == null)
            {
                throw new InvalidOperationException("Model not initialized. Call InitializeAsync first.");
            }

            try
            {
                _logger.LogInformation($"Generating icon with prompt: {prompt}");

                // Create input tensors
                var inputs = new List<NamedOnnxValue>();

                // TODO: Implement tensor creation from prompt and settings
                // Example: Create input tensor for prompt
                var promptTensor = new DenseTensor<string>(new[] { prompt }, new[] { 1 });
                inputs.Add(NamedOnnxValue.CreateFromTensor("prompt", promptTensor));

                // Run inference
                using var outputs = _session.Run(inputs);

                // TODO: Convert output tensors to image
                // This will depend on the specific ONNX model format being used

                // Placeholder for now
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate icon");
                throw;
            }
        }

        public async Task<byte[]> RefineIconAsync(byte[] sourceImage, string prompt, float strength = 0.75f)
        {
            if (_session == null)
            {
                throw new InvalidOperationException("Model not initialized. Call InitializeAsync first.");
            }

            try
            {
                _logger.LogInformation($"Refining icon with prompt: {prompt}");

                // TODO: Implement image-to-image generation
                // This will depend on the specific ONNX model format being used

                // Placeholder for now
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refine icon");
                throw;
            }
        }

        public async Task SaveIconAsync(StorageFile destinationFile)
        {
            try
            {
                var tempFolder = await ApplicationData.Current.TemporaryFolder.GetFolderAsync("GeneratedIcons");
                var files = await tempFolder.GetFilesAsync();
                if (files.Count > 0)
                {
                    // Get the most recently generated icon
                    var sourceFile = files[files.Count - 1];
                    await sourceFile.CopyAndReplaceAsync(destinationFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save icon");
                throw;
            }
        }

        public async Task CleanupTempFilesAsync()
        {
            try
            {
                var tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                    "GeneratedIcons",
                    CreationCollisionOption.OpenIfExists);
                    
                var files = await tempFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    try
                    {
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete temporary file: {file.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temporary files");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _session?.Dispose();
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