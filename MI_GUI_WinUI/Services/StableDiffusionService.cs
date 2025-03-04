﻿using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.Services.StableDiffusion;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MI_GUI_WinUI.Services
{
    public class StableDiffusionService : IStableDiffusionService, IDisposable
    {
        private readonly ILogger<StableDiffusionService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _baseDirectory;
        private const string MODEL_FOLDER = "AI_Models/StableDiffusion";
        private readonly object _initLock = new();

        private StableDiffusionStudio? _studio;
        private StableDiffusionConfig? _config;

        public StableDiffusionService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<StableDiffusionService>();
            
            _baseDirectory = Path.GetFullPath(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, MODEL_FOLDER));
            _logger.LogInformation("Base directory set to: {path}", _baseDirectory);
        }

        public bool IsInitialized => _studio != null && _config != null;

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
                var modelPath = Path.Combine(_baseDirectory);

                // Validate model directory exists
                if (!Directory.Exists(modelPath))
                {
                    _logger.LogError("Model directory not found: {path}", modelPath);
                    throw new DirectoryNotFoundException($"Model directory not found: {modelPath}");
                }

                // Create configuration
                _config = new StableDiffusionConfig
                {
                    ModelBasePath = modelPath,
                    Height = 512,
                    Width = 512,
                    NumInferenceSteps = 20,
                    GuidanceScale = 7.5f,
                    ExecutionProvider = !useGpu ? ExecutionProvider.CPU : ExecutionProvider.DirectML
                };

                // Initialize model paths
                _config.InitializePaths();
                _config.ValidateModelFiles();

                // Create studio in async task to avoid blocking
                await Task.Run(() => 
                {
                    lock (_initLock)
                    {
                        if (!IsInitialized)
                        {
                            _studio = new StableDiffusionStudio(_config, _loggerFactory);
                        }
                    }
                });

                _logger.LogInformation("Initialization completed successfully");
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
                _studio?.Dispose();
                _studio = null;
                _config = null;
            }
        }

        public async Task<byte[]> GenerateImage(IconGenerationSettings settings, IProgress<int>? progress = null)
        {
            if (!IsInitialized || _studio == null)
                throw new InvalidOperationException("Service not initialized. Call Initialize first.");

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            void ReportProgress(int value)
            {
                progress?.Report(value);
            }

            try
            {
                ReportProgress(0);

                // Update configuration if needed
                if (settings.Width > 0) _config.Width = settings.Width;
                if (settings.Height > 0) _config.Height = settings.Height;
                if (settings.NumInferenceSteps > 0) _config.NumInferenceSteps = settings.NumInferenceSteps;
                if (settings.GuidanceScale > 0) _config.GuidanceScale = settings.GuidanceScale;

                // Start generation
                ReportProgress(10);

                // Map progress to 20-90 range
                var progressTracker = new Progress<double>(value =>
                {
                    var mappedProgress = 20 + (int)(value * 70);
                    ReportProgress(mappedProgress);
                });

                var image = await _studio.GenerateImageAsync(settings.Prompt, progressTracker, settings.CancellationToken);

                ReportProgress(90);

                using var memStream = new MemoryStream();
                await image.SaveAsPngAsync(memStream);

                ReportProgress(100);
                return memStream.ToArray();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Generation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate image");
                throw;
            }
        }

        public async Task<(bool success, string path)> GenerateIconAsync(string prompt, IProgress<GenerationProgress> progress, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

                var settings = new IconGenerationSettings
                {
                    Prompt = prompt,
                    Width = 512,
                    Height = 512,
                    NumInferenceSteps = 20,
                    GuidanceScale = 7.5f,
                    CancellationToken = cancellationToken
                };

                ReportProgress(0.0f, GenerationProgressState.Loading, progress);

                // Generate image
                var imageData = await GenerateImage(settings, new Progress<int>(value =>
                {
                    var p = value / 100.0f;
                    var state = value switch
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
                    ReportProgress(p, state, progress);
                }));

                // Save image
                var outputPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "MotionInput",
                    "data", 
                    "assets",
                    "generated_icons",
                    $"icon_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                );

                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(outputPath, imageData, cancellationToken);

                ReportProgress(1.0f, GenerationProgressState.Completed, progress);
                return (true, outputPath);
            }
            catch (OperationCanceledException)
            {
                ReportProgress(0f, GenerationProgressState.Cancelled, progress);
                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate and save icon");
                ReportProgress(0f, GenerationProgressState.Failed, progress);
                return (false, string.Empty);
            }
        }

        private void ReportProgress(float value, GenerationProgressState state, IProgress<GenerationProgress> progress)
        {
            progress?.Report(new GenerationProgress { Progress = value, State = state });
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
                    _studio?.Dispose();
                    _studio = null;
                    _config = null;
                }
            }
        }
    }
}
