using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MI_GUI_WinUI.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
        private readonly Random _random = new();

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

        public async Task<byte[]> GenerateImage(IconGenerationSettings settings, IProgress<int>? progress = null)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("StableDiffusion service is not initialized");

            try
            {
                // 1. Tokenize input prompt
                var inputTokens = await TokenizeText(settings.Prompt);
                progress?.Report(5);

                // 2. Encode text
                var textEmbeddings = await EncodeText(inputTokens);
                progress?.Report(10);

                // 3. Generate latent noise with seed
                var latents = GenerateInitialNoise(seed: settings.Seed);
                progress?.Report(15);

                // 4. Run UNet inference with guidance scale
                var finalLatents = await RunDiffusion(latents, textEmbeddings, 
                    settings.NumInferenceSteps, settings.GuidanceScale, progress);

                // 5. Decode latents to image using VAE
                progress?.Report(85);
                var imageData = await DecodeLatents(finalLatents);

                // 6. Apply circular mask and resize
                progress?.Report(95);
                var result = ApplyCircularMask(imageData, settings.ImageSize);
                progress?.Report(100);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image with prompt: {prompt}", settings.Prompt);
                throw;
            }
        }

        private async Task<long[]> TokenizeText(string text)
        {
            if (_tokenizer == null)
                throw new InvalidOperationException("Tokenizer not initialized");

            try
            {
                // Create input tensor
                var inputTensor = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("string_input", 
                        new DenseTensor<string>(new string[] { text }, new[] { 1 }))
                };

                // Run tokenizer
                using var output = await Task.Run(() => _tokenizer.Run(inputTensor));

                // Get input IDs from output
                var inputIds = output.First(x => x.Name == "input_ids")
                    .AsEnumerable<long>()
                    .Take(77) // Max length for Stable Diffusion
                    .ToArray();

                _logger.LogDebug("Text tokenized successfully: {text}", text);
                return inputIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text tokenization");
                throw;
            }
        }

        private async Task<float[]> EncodeText(long[] tokens)
        {
            if (_textEncoder == null)
                throw new InvalidOperationException("Text encoder not initialized");

            try
            {
                // Create input tensor for text encoder
                var inputTensor = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", 
                        new DenseTensor<long>(tokens, new[] { 1, tokens.Length }))
                };

                // Run text encoder
                using var output = await Task.Run(() => _textEncoder.Run(inputTensor));

                // Get last hidden state (text embeddings)
                var lastHiddenState = output.First(x => x.Name == "last_hidden_state")
                    .AsEnumerable<float>()
                    .ToArray();

                _logger.LogDebug("Text encoded successfully, shape: {length}", lastHiddenState.Length);
                return lastHiddenState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text encoding");
                throw;
            }
        }

        private float[] GenerateInitialNoise(
            int batchSize = 1, int height = 64, int width = 64, int channels = 4, long seed = -1)
        {
            var latentSize = batchSize * channels * height * width;
            var latents = new float[latentSize];
            var random = seed < 0 ? _random : new Random((int)seed);

            for (int i = 0; i < latentSize; i++)
            {
                latents[i] = (float)(random.NextDouble() * 2 - 1); // Random values between -1 and 1
            }
            return latents;
        }

        private async Task<float[]> RunDiffusion(
            float[] initialLatents, float[] textEmbeddings, int numInferenceSteps, float guidanceScale,
            IProgress<int>? progress = null)
        {
            if (_unet == null)
                throw new InvalidOperationException("UNet not initialized");

            try
            {
                var latents = initialLatents;
                _logger.LogInformation("Starting diffusion with {steps} steps, guidance scale: {scale}", 
                    numInferenceSteps, guidanceScale);
                var timesteps = GenerateTimesteps(numInferenceSteps);

                for (int i = 0; i < timesteps.Length; i++)
                {
                    var t = timesteps[i];
                    
                    // Prepare input tensors for UNet
                    var inputTensors = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("sample", 
                            new DenseTensor<float>(latents, new[] { 1, 4, 64, 64 })),
                        NamedOnnxValue.CreateFromTensor("timestep", 
                            new DenseTensor<long>(new[] { t }, new[] { 1 })),
                        NamedOnnxValue.CreateFromTensor("encoder_hidden_states", 
                            new DenseTensor<float>(textEmbeddings, new[] { 1, 77, 768 }))
                    };

                    // Prepare unconditional input (empty embedding)
                    var uncondEmbedding = new float[textEmbeddings.Length];
                    var uncondInput = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("sample", 
                            new DenseTensor<float>(latents, new[] { 1, 4, 64, 64 })),
                        NamedOnnxValue.CreateFromTensor("timestep", 
                            new DenseTensor<long>(new[] { t }, new[] { 1 })),
                        NamedOnnxValue.CreateFromTensor("encoder_hidden_states", 
                            new DenseTensor<float>(uncondEmbedding, new[] { 1, 77, 768 }))
                    };

                    // Get both conditional and unconditional predictions
                    using var uncondOutput = await Task.Run(() => _unet.Run(uncondInput));
                    using var condOutput = await Task.Run(() => _unet.Run(inputTensors));

                    var uncondPred = uncondOutput.First(x => x.Name == "out_sample")
                        .AsEnumerable<float>()
                        .ToArray();
                    var condPred = condOutput.First(x => x.Name == "out_sample")
                        .AsEnumerable<float>()
                        .ToArray();

                    // Apply classifier-free guidance
                    var noisePred = new float[uncondPred.Length];
                    for (int j = 0; j < noisePred.Length; j++)
                    {
                        noisePred[j] = uncondPred[j] + guidanceScale * (condPred[j] - uncondPred[j]);
                    }

                    // Update latents using scheduler step
                    latents = SchedulerStep(latents, noisePred, t);

                    _logger.LogDebug("Completed diffusion step {i} of {total}", i + 1, timesteps.Length);
                    
                    // Report progress from 20% to 80%
                    var progressValue = 20 + (60 * (i + 1) / timesteps.Length);
                    progress?.Report(progressValue);
                }

                return latents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during diffusion process");
                throw;
            }
        }

        private long[] GenerateTimesteps(int numInferenceSteps)
        {
            // Generate timesteps for DDIM scheduler
            var timesteps = new long[numInferenceSteps];
            var step = 1000 / numInferenceSteps;
            for (int i = 0; i < numInferenceSteps; i++)
            {
                timesteps[i] = 1000 - (i * step);
            }
            return timesteps;
        }

        private float[] SchedulerStep(float[] latents, float[] noisePred, long timestep)
        {
            // DDIM scheduler parameters
            float beta1 = 0.00085f;
            float beta2 = 0.012f;
            float betaSchedule = beta1 + (beta2 - beta1) * (timestep / 1000.0f);
            
            // Calculate alpha values
            float alpha = 1.0f - betaSchedule;
            float alphaProd = (float)Math.Pow(alpha, timestep);
            float alphaProdPrev = timestep > 0 ? (float)Math.Pow(alpha, timestep - 1) : 1.0f;
            
            // Calculate coefficients
            float sigma = (float)Math.Sqrt((1 - alphaProdPrev) / (1 - alphaProd) * betaSchedule);
            float c1 = (float)Math.Sqrt(1 / alpha);
            float c2 = (float)Math.Sqrt(1 - alpha) * sigma;
            
            var result = new float[latents.Length];
            for (int i = 0; i < latents.Length; i++)
            {
                // Predict x0
                float predX0 = (latents[i] - c2 * noisePred[i]) / c1;
                
                // Update latent
                result[i] = c1 * predX0 + c2 * noisePred[i];
            }

            return result;
        }

        private async Task<byte[]> DecodeLatents(float[] latents)
        {
            if (_vae == null)
                throw new InvalidOperationException("VAE not initialized");

            try
            {
                // Scale latents for VAE input
                for (int i = 0; i < latents.Length; i++)
                {
                    latents[i] /= 0.18215f;
                }

                // Create input tensor for VAE
                var inputTensor = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("latent_sample",
                        new DenseTensor<float>(latents, new[] { 1, 4, 64, 64 }))
                };

                // Run VAE decoder
                using var output = await Task.Run(() => _vae.Run(inputTensor));

                // Get the decoded image tensor
                var imageArray = output.First(x => x.Name == "sample")
                    .AsEnumerable<float>()
                    .ToArray();

                // Convert to image format (0-255 range)
                var rgbImage = new byte[256 * 256 * 3];
                for (int i = 0; i < imageArray.Length; i++)
                {
                    var value = (byte)Math.Clamp((imageArray[i] + 1) * 127.5f, 0, 255);
                    rgbImage[i] = value;
                }

                // Create bitmap from raw bytes
                using var bitmap = new Bitmap(256, 256, PixelFormat.Format24bppRgb);
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format24bppRgb);

                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(rgbImage, 0, bitmapData.Scan0, rgbImage.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // Convert to PNG
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding latents");
                throw;
            }
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

        private byte[] ApplyCircularMask(byte[] imageData, Size size)
        {
            using var inputStream = new MemoryStream(imageData);
            using var inputBitmap = new Bitmap(inputStream);
            using var outputBitmap = new Bitmap(size.Width, size.Height);
            using var graphics = Graphics.FromImage(outputBitmap);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Create and set circular clipping path
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, size.Width, size.Height);
            graphics.SetClip(path);

            // Draw the image
            graphics.DrawImage(inputBitmap, 0, 0, size.Width, size.Height);

            // Convert to PNG
            using var stream = new MemoryStream();
            outputBitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
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

        public void Dispose()
        {
            _unet?.Dispose();
            _tokenizer?.Dispose();
            _textEncoder?.Dispose();
            _vae?.Dispose();
        }
    }
}
