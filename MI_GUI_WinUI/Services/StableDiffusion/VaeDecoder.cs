using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public class VaeDecoder : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly ILogger<VaeDecoder> _logger;
        private readonly StableDiffusionConfig _config;
        private bool _disposedValue;

        public VaeDecoder(StableDiffusionConfig config, ILogger<VaeDecoder> logger)
        {
            _config = config;
            _logger = logger;
            _session = new InferenceSession(config.VaeDecoderPath, config.GetSessionOptionsForEp());
        }

        public Image<Rgba32> Decode(DenseTensor<float> latents)
        {
            try
            {
                _logger.LogDebug("Starting VAE decoding");
                
                // Scale latents
                var scaledLatents = new DenseTensor<float>(latents.ToArray().Select(x => x / 0.18215f).ToArray(), latents.Dimensions.ToArray());
                
                // Create model input
                var input = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("latent", scaledLatents)
                };

                _logger.LogDebug("Running VAE decoder inference");
                using var output = _session.Run(input);

                // Get output tensor and convert to float array
                var outputTensor = output.First().Value as DenseTensor<float>;
                if (outputTensor == null)
                {
                    throw new InvalidOperationException("Failed to get output tensor from VAE decoder");
                }

                _logger.LogDebug("Converting VAE output to image");
                return ConvertToImage(outputTensor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VAE decoding");
                throw;
            }
        }

        private Image<Rgba32> ConvertToImage(DenseTensor<float> tensor)
        {
            // Expected shape: [batch, channels, height, width]
            var dimensions = tensor.Dimensions.ToArray();
            if (dimensions.Length != 4 || dimensions[1] != 3)
            {
                var shape = string.Join("x", dimensions.Select(d => d.ToString()));
                throw new ArgumentException($"Unexpected tensor dimensions: {shape}");
            }

            var height = dimensions[2];
            var width = dimensions[3];
            var image = new Image<Rgba32>(width, height);

            // Convert to pixel values
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    // Get RGB values from tensor (in CHW format)
                    var r = ConvertToPixelValue(tensor[0, 0, h, w]);
                    var g = ConvertToPixelValue(tensor[0, 1, h, w]);
                    var b = ConvertToPixelValue(tensor[0, 2, h, w]);

                    image[w, h] = new Rgba32(r, g, b, 255);
                }
            }

            return image;
        }

        private byte ConvertToPixelValue(float value)
        {
            // Convert from [-1,1] to [0,255]
            var pixel = (value + 1f) * 127.5f;
            return (byte)Math.Clamp(Math.Round(pixel), 0, 255);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _session?.Dispose();
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
