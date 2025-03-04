using Microsoft.ML.OnnxRuntime;
using System;
using System.IO;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public class StableDiffusionConfig
    {
        private string _modelBasePath;
        private string _textEncoderPath;
        private string _unetPath;
        private string _vaeDecoderPath;
        private int _height;
        private int _width;
        private int _numInferenceSteps;
        private double _guidanceScale;

        // Added execution provider properties
        public ExecutionProvider ExecutionProvider { get; set; } = ExecutionProvider.CPU;
        public string ExecutionProviderTarget { get; set; } = string.Empty;

        public string ModelBasePath
        {
            get => _modelBasePath;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Model base path cannot be empty");
                if (!Directory.Exists(value))
                    throw new DirectoryNotFoundException($"Model directory not found: {value}");
                _modelBasePath = value;
            }
        }

        public string TextEncoderPath
        {
            get => _textEncoderPath;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Text encoder path cannot be empty");
                if (!File.Exists(value))
                    throw new FileNotFoundException($"Text encoder model not found: {value}");
                _textEncoderPath = value;
            }
        }

        public string UnetPath
        {
            get => _unetPath;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("UNet path cannot be empty");
                if (!File.Exists(value))
                    throw new FileNotFoundException($"UNet model not found: {value}");
                _unetPath = value;
            }
        }

        public string VaeDecoderPath
        {
            get => _vaeDecoderPath;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("VAE decoder path cannot be empty");
                if (!File.Exists(value))
                    throw new FileNotFoundException($"VAE decoder model not found: {value}");
                _vaeDecoderPath = value;
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Height must be positive");
                if (value % 8 != 0)
                    throw new ArgumentException("Height must be divisible by 8");
                _height = value;
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Width must be positive");
                if (value % 8 != 0)
                    throw new ArgumentException("Width must be divisible by 8");
                _width = value;
            }
        }

        public int NumInferenceSteps
        {
            get => _numInferenceSteps;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Number of inference steps must be positive");
                if (value > 1000)
                    throw new ArgumentException("Number of inference steps cannot exceed 1000");
                _numInferenceSteps = value;
            }
        }

        public double GuidanceScale
        {
            get => _guidanceScale;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Guidance scale must be positive");
                if (value > 20)
                    throw new ArgumentException("Guidance scale cannot exceed 20");
                _guidanceScale = value;
            }
        }

        public SessionOptions GetSessionOptionsForEp()
        {
            try
            {
                var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    IntraOpNumThreads = Environment.ProcessorCount,
                    InterOpNumThreads = 1  // Minimize thread contention
                };

                // Configure execution provider
                switch (ExecutionProvider)
                {
                    case ExecutionProvider.CUDA:
                        options.AppendExecutionProvider_CUDA();
                        break;
                    case ExecutionProvider.DirectML:
                        options.AppendExecutionProvider_DML();
                        break;
                    case ExecutionProvider.CPU:
                    default:
                        options.AppendExecutionProvider_CPU();
                        break;
                }

                return options;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create session options", ex);
            }
        }

        public void InitializePaths()
        {
            if (string.IsNullOrEmpty(ModelBasePath))
                throw new InvalidOperationException("Model base path not set");

            if (string.IsNullOrEmpty(_textEncoderPath))
                _textEncoderPath = Path.Combine(ModelBasePath, "text_encoder.onnx");
            if (string.IsNullOrEmpty(_unetPath))
                _unetPath = Path.Combine(ModelBasePath, "unet.onnx");
            if (string.IsNullOrEmpty(_vaeDecoderPath))
                _vaeDecoderPath = Path.Combine(ModelBasePath, "vae_decoder.onnx");
        }

        public void ValidateModelFiles()
        {
            if (!File.Exists(_textEncoderPath))
                throw new FileNotFoundException("Text encoder model not found", _textEncoderPath);
            if (!File.Exists(_unetPath))
                throw new FileNotFoundException("UNet model not found", _unetPath);
            if (!File.Exists(_vaeDecoderPath))
                throw new FileNotFoundException("VAE decoder model not found", _vaeDecoderPath);

            if (_height <= 0 || _height % 8 != 0)
                throw new InvalidOperationException("Invalid height value");
            if (_width <= 0 || _width % 8 != 0)
                throw new InvalidOperationException("Invalid width value");
            if (_numInferenceSteps <= 0 || _numInferenceSteps > 1000)
                throw new InvalidOperationException("Invalid number of inference steps");
            if (_guidanceScale <= 0 || _guidanceScale > 20)
                throw new InvalidOperationException("Invalid guidance scale");
        }
    }

    public enum ExecutionProvider
    {
        CPU,
        CUDA,
        DirectML
    }
}
