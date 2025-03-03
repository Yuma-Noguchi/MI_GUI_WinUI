using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using MI_GUI_WinUI.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MI_GUI_WinUI.Services
{
    public class StableDiffusionService : IStableDiffusionService, IDisposable
    {
        private readonly ILogger<StableDiffusionService> _logger;
        private InferenceSession? _unet;
        private InferenceSession? _tokenizer;
        private InferenceSession? _textEncoder;
        private InferenceSession? _vae;
        private bool _useGpu;

        public bool IsInitialized { get; private set; }

        public StableDiffusionService(ILogger<StableDiffusionService> logger)
        {
            _logger = logger;
        }

        public async Task Initialize(bool useGpu)
        {
            try
            {
                _useGpu = useGpu;
                await LoadModel();
                IsInitialized = true;
                _logger.LogInformation("StableDiffusion service initialized successfully with GPU: {useGpu}", useGpu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize StableDiffusion service");
                throw;
            }
        }

        public async Task<byte[]> GenerateImage(IconGenerationSettings settings)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("StableDiffusion service is not initialized");
            }

            try
            {
                // TODO: Implement the actual generation pipeline
                // For now, return a placeholder circular image
                return await Task.Run(() => CreatePlaceholderImage(settings.ImageSize));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image with prompt: {prompt}", settings.Prompt);
                throw;
            }
        }

        private async Task LoadModel()
        {
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI_Models", "StableDiffusion");
            if (!Directory.Exists(modelPath))
            {
                throw new DirectoryNotFoundException($"Model directory not found: {modelPath}");
            }

            var sessionOptions = new SessionOptions();
            if (_useGpu)
            {
                try
                {
                    sessionOptions.AppendExecutionProvider_DML(0);
                    _logger.LogInformation("Using DirectML (GPU) execution provider");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize DirectML provider, falling back to CPU");
                    _useGpu = false;
                }
            }
            else
            {
                _logger.LogInformation("Using CPU execution provider");
            }

            try
            {
                _unet = await Task.Run(() => new InferenceSession(
                    Path.Combine(modelPath, "unet.onnx"), 
                    sessionOptions));
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogError(ex, "ONNX Runtime DLL not found. Please ensure Microsoft.ML.OnnxRuntime DLLs are present");
                throw new InvalidOperationException("ONNX Runtime components are missing. Please reinstall the application.", ex);
            }

            _tokenizer = await Task.Run(() => new InferenceSession(
                Path.Combine(modelPath, "tokenizer.onnx"), 
                sessionOptions));

            _textEncoder = await Task.Run(() => new InferenceSession(
                Path.Combine(modelPath, "text_encoder.onnx"), 
                sessionOptions));

            _vae = await Task.Run(() => new InferenceSession(
                Path.Combine(modelPath, "vae.onnx"), 
                sessionOptions));
        }

        private byte[] CreatePlaceholderImage(Size size)
        {
            using var bitmap = new Bitmap(size.Width, size.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Create circular mask
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, size.Width, size.Height);
            graphics.SetClip(path);

            // Fill with a gradient
            using var brush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(size.Width, size.Height),
                Color.LightBlue,
                Color.DarkBlue);
            graphics.FillEllipse(brush, 0, 0, size.Width, size.Height);

            // Convert to bytes
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        public void Dispose()
        {
            _unet?.Dispose();
            _tokenizer?.Dispose();
            _textEncoder?.Dispose();
            _vae?.Dispose();
        }
    }
}
