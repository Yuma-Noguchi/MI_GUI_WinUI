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
        private CLIPTokenizer? _tokenizer;
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
                var inputTokens = TokenizeText(settings.Prompt);
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

        private long[] TokenizeText(string text)
        {
            if (_tokenizer == null)
                throw new InvalidOperationException("Tokenizer not initialized");

            try
            {
                var tokens = _tokenizer.Tokenize(text);
                _logger.LogDebug("Text tokenized successfully: {text}", text);
                return tokens;
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
                var inputTensor = new NamedOnnxValue[]
                {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(tokens, new[] { 1, tokens.Length }))
                };

                // Run text encoder
                using var output = await Task.Run(() => _textEncoder.Run(inputTensor));

                // Debug log available output names (helpful for debugging)
                _logger.LogDebug("Text encoder output names: {names}", string.Join(", ", output.Select(x => x.Name)));

                // Directly try to get "text_embeddings" output
                NamedOnnxValue embeddingOutput = null;
                try
                {
                    embeddingOutput = output.First(x => x.Name == "text_embeddings");
                }
                catch (InvalidOperationException)
                {
                    // Log a warning if "text_embeddings" is not found, fallback to first output
                    _logger.LogWarning("Output 'text_embeddings' not found. Using fallback output.");
                    embeddingOutput = output.First();
                    _logger.LogDebug("Using fallback output tensor with name: {name}", embeddingOutput.Name); // Optional: Log fallback name
                }

                // Get embeddings as float array
                var lastHiddenState = embeddingOutput.AsEnumerable<float>().ToArray();
                _logger.LogDebug("Text encoded successfully, shape: {length}", lastHiddenState.Length);

                return lastHiddenState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text encoding: {message}", ex.Message);
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
                        NamedOnnxValue.CreateFromTensor("latent", 
                            new DenseTensor<float>(latents, new[] { 1, 4, 64, 64 })),
                        NamedOnnxValue.CreateFromTensor("timestep", 
                            new DenseTensor<long>(new[] { t }, new[] { 1 })),
                        NamedOnnxValue.CreateFromTensor("text_embed", 
                            new DenseTensor<float>(textEmbeddings, new[] { 1, 77, 768 }))
                    };

                    // Prepare unconditional input (empty embedding)
                    var uncondEmbedding = new float[textEmbeddings.Length];
                    var uncondInput = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("latent", 
                            new DenseTensor<float>(latents, new[] { 1, 4, 64, 64 })),
                        NamedOnnxValue.CreateFromTensor("timestep", 
                            new DenseTensor<long>(new[] { t }, new[] { 1 })),
                        NamedOnnxValue.CreateFromTensor("text_embed", 
                            new DenseTensor<float>(uncondEmbedding, new[] { 1, 77, 768 }))
                    };

                    // Get both conditional and unconditional predictions
                    using var uncondOutput = await Task.Run(() => _unet.Run(uncondInput));
                    using var condOutput = await Task.Run(() => _unet.Run(inputTensors));

                    var uncondPred = uncondOutput.First(x => x.Name == "noise_pred")
                        .AsEnumerable<float>()
                        .ToArray();
                    var condPred = condOutput.First(x => x.Name == "noise_pred")
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
                    NamedOnnxValue.CreateFromTensor("latent",
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
                int pixelIndex = 0; // Index for rgbImage (0 to 196607)
                for (int y = 0; y < 256; y++) // Iterate over image height (rows)
                {
                    for (int x = 0; x < 256; x++) // Iterate over image width (columns)
                    {
                        // Calculate index in imageArray for the current pixel (x, y) and channel
                        int arrayIndexBase = (y * 256 + x) * 4; // Base index for 4 channels

                        // Assuming channels are ordered like R, G, B, A (or similar - check VAE output docs if available)
                        // Extract R, G, B channels from imageArray (take first 3 channels, ignore 4th if needed)
                        float rFloat = imageArray[arrayIndexBase + 0];
                        float gFloat = imageArray[arrayIndexBase + 1];
                        float bFloat = imageArray[arrayIndexBase + 2];
                        // float aFloat = imageArray[arrayIndexBase + 3]; // If you want to use alpha or inspect it

                        // Convert to byte and clamp for R channel
                        rgbImage[pixelIndex++] = (byte)Math.Clamp((rFloat + 1) * 127.5f, 0, 255); // R
                        rgbImage[pixelIndex++] = (byte)Math.Clamp((gFloat + 1) * 127.5f, 0, 255); // G
                        rgbImage[pixelIndex++] = (byte)Math.Clamp((bFloat + 1) * 127.5f, 0, 255); // B
                    }
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
            var modelPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "AI_Models", "StableDiffusion");
            _logger.LogInformation("Looking for AI_Models in base directory: {baseDir}", modelPath);

            if (!Directory.Exists(modelPath))
            {
                throw new DirectoryNotFoundException(
                    $"Could not find AI_Models directory in any of the following locations:{Environment.NewLine}{modelPath}");
            }

            _logger.LogInformation("Using model path: {modelPath}", modelPath);

            var requiredFiles = new[] { "text_encoder.onnx", "unet.onnx", "vae_decoder.onnx", "vocab.json", "merges.txt" };
            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(modelPath, file);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Required model file not found: {file}", filePath);
                }
                _logger.LogInformation("Found required model file: {file}", file);
            }

            _logger.LogInformation("Configuring inference session options...");
            var sessionOptions = new SessionOptions();
            if (_useGpu)
            {
                try
                {
                    _logger.LogInformation("Attempting to initialize DirectML provider...");
                    sessionOptions.AppendExecutionProvider_DML(0);
                    _logger.LogInformation("Successfully initialized DirectML (GPU) execution provider");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize DirectML provider, falling back to CPU. Error: {message}", ex.Message);
                    _useGpu = false;
                }
            }

            if (!_useGpu)
            {
                _logger.LogInformation("Using CPU execution provider");
                sessionOptions.EnableMemoryPattern = true;
                sessionOptions.EnableCpuMemArena = true;
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

            // Initialize the C# tokenizer instead of using ONNX
            _tokenizer = new CLIPTokenizer(modelPath);

            _textEncoder = await Task.Run(() => new InferenceSession(
                Path.Combine(modelPath, "text_encoder.onnx"), 
                sessionOptions));

            _vae = await Task.Run(() => new InferenceSession(
                Path.Combine(modelPath, "vae_decoder.onnx"), 
                sessionOptions));
        }

        public void Dispose()
        {
            _unet?.Dispose();
            _textEncoder?.Dispose();
            _vae?.Dispose();
        }
    }
}
