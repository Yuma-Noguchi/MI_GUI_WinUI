using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public class TextProcessor : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly ILogger<TextProcessor> _logger;
        private readonly CLIPTokenizer _tokenizer;
        private bool _disposedValue;
        private const int MaxTokenLength = 77;

        public TextProcessor(StableDiffusionConfig config, ILogger<TextProcessor> logger)
        {
            try
            {
                if (string.IsNullOrEmpty(config.TextEncoderPath))
                    throw new ArgumentException("Text encoder path not specified", nameof(config));
                if (string.IsNullOrEmpty(config.ModelBasePath))
                    throw new ArgumentException("Model base path not specified", nameof(config));

                if (!File.Exists(config.TextEncoderPath))
                    throw new FileNotFoundException("Text encoder model not found", config.TextEncoderPath);

                _session = new InferenceSession(config.TextEncoderPath, config.GetSessionOptionsForEp());
                _tokenizer = new CLIPTokenizer(config.ModelBasePath);
                _logger = logger;

                ValidateModel();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize text processor");
                throw;
            }
        }

        private void ValidateModel()
        {
            var inputs = _session.InputMetadata;
            if (!inputs.ContainsKey("input_ids"))
            {
                throw new InvalidOperationException("Text encoder model missing required input: input_ids");
            }

            var outputs = _session.OutputMetadata;
            if (outputs.Count == 0)
            {
                throw new InvalidOperationException("Text encoder model has no outputs defined");
            }
        }

        public DenseTensor<float> ProcessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));

            try
            {
                _logger.LogDebug("Tokenizing text: {text}", text);
                
                // Tokenize text with fixed length
                var tokens = _tokenizer.Tokenize(text, MaxTokenLength);
                if (tokens == null || tokens.Length == 0)
                    throw new InvalidOperationException("Tokenization failed - no tokens generated");

                _logger.LogDebug("Text tokenized into {count} tokens", tokens.Length);

                // Create input tensor with padding
                var paddedTokens = new long[MaxTokenLength];
                Array.Copy(tokens, paddedTokens, Math.Min(tokens.Length, MaxTokenLength));

                // Create input tensor
                var inputDimensions = new[] { 1, MaxTokenLength };
                var inputTensor = TensorHelper.CreateTensor(paddedTokens, inputDimensions);

                // Prepare model input
                var input = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
                };

                // Run text encoder
                _logger.LogDebug("Running text encoder inference");
                using var output = _session.Run(input);
                
                // Get last hidden states (text embeddings)
                var outputTensor = output.First().Value as DenseTensor<float>;
                if (outputTensor == null)
                {
                    throw new InvalidOperationException("Failed to get output tensor from text encoder");
                }

                var output_test = new DenseTensor<float>(outputTensor.ToArray(), new[] { outputTensor.Dimensions[0], outputTensor.Dimensions[1], outputTensor.Dimensions[2] });

                // Log dimensions for debugging
                var dimensions = outputTensor.Dimensions.ToArray();
                var shape = string.Join("x", dimensions.Select(d => d.ToString()));
                _logger.LogDebug("Text encoded with shape: {shape}", shape);

                // Validate embedding dimensions
                ValidateOutputDimensions(dimensions);

                return output_test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text: {text}", text);
                throw;
            }
        }

        private void ValidateOutputDimensions(int[] dimensions)
        {
            if (dimensions.Length != 3)
            {
                throw new InvalidOperationException(
                    $"Unexpected embedding dimensions: [{string.Join(",", dimensions)}], expected [batch, sequence_length, embedding_dim]");
            }

            if (dimensions[0] != 1)
            {
                throw new InvalidOperationException(
                    $"Invalid batch size: {dimensions[0]}, expected 1");
            }

            if (dimensions[1] != MaxTokenLength)
            {
                throw new InvalidOperationException(
                    $"Invalid sequence length: {dimensions[1]}, expected {MaxTokenLength}");
            }

            if (dimensions[2] != 768)  // CLIP's hidden dimension size
            {
                throw new InvalidOperationException(
                    $"Invalid embedding dimension: {dimensions[2]}, expected 768");
            }
        }

        public virtual void Dispose(bool disposing)
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
