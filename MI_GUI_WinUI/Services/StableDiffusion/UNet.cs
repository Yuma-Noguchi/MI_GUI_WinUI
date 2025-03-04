using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public class UNet : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly float _guidanceScale;
        private readonly ILogger<UNet> _logger;
        private bool _disposedValue;

        public UNet(StableDiffusionConfig config, ILogger<UNet> logger)
        {
            try
            {
                _session = new InferenceSession(config.UnetPath, config.GetSessionOptionsForEp());
                _guidanceScale = (float)config.GuidanceScale;
                _logger = logger;

                ValidateModel();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize UNet");
                throw;
            }
        }

        private void ValidateModel()
        {
            var inputs = _session.InputMetadata;
            var requiredInputs = new[] { "text_embed", "latent", "timestep" };
            foreach (var input in requiredInputs)
            {
                if (!inputs.ContainsKey(input))
                {
                    throw new InvalidOperationException($"UNet model missing required input: {input}");
                }
            }
        }

        public DenseTensor<float> Predict(DenseTensor<float> encoderHiddenStates, DenseTensor<float> sample, long timestep)
        {
            if (encoderHiddenStates == null)
                throw new ArgumentNullException(nameof(encoderHiddenStates));
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));

            try
            {
                ValidateInputDimensions(encoderHiddenStates, sample);

                _logger.LogDebug("Creating UNet model input");
                var input = CreateModelInput(encoderHiddenStates, sample, timestep);

                // Run inference for both conditional and unconditional
                _logger.LogDebug("Running UNet inference");
                using var output = _session.Run(input);
                
                var outputTensor = output.First().Value as DenseTensor<float>;
                if (outputTensor == null)
                {
                    throw new InvalidOperationException("Failed to get output tensor from UNet");
                }

                var outputShape = FormatDimensions(outputTensor.Dimensions);
                _logger.LogDebug("UNet output shape: {shape}", outputShape);

                // Expected output shape should match doubled input with channels
                var expectedShape = new[] { 2, 4, sample.Dimensions[2], sample.Dimensions[3] };
                ValidateOutputShape(outputTensor, expectedShape);

                // Split output for conditional and unconditional
                var splitDimensions = new[] { 1, 4, sample.Dimensions[2], sample.Dimensions[3] };
                var (noisePred, noisePredText) = TensorHelper.SplitTensor(outputTensor, splitDimensions);

                // Apply classifier-free guidance
                var result = ApplyGuidance(noisePred, noisePredText);
                
                var resultShape = FormatDimensions(result.Dimensions);
                _logger.LogDebug("Guidance applied, result shape: {shape}", resultShape);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during UNet inference at timestep {timestep}", timestep);
                throw;
            }
        }

        private List<NamedOnnxValue> CreateModelInput(DenseTensor<float> encoderHiddenStates, DenseTensor<float> sample, long timestep)
        {
            try
            {
                // First convert tensors to arrays
                var sampleArray = sample.ToArray();
                var encoderArray = encoderHiddenStates.ToArray();
                
                // Create duplicated arrays
                var batchedSample = TensorHelper.Duplicate(sampleArray, new[] { 2, sample.Dimensions[1], sample.Dimensions[2], sample.Dimensions[3] });
                var batchedEmbeddings = TensorHelper.Duplicate(encoderArray, new[] { 2, encoderHiddenStates.Dimensions[1], encoderHiddenStates.Dimensions[2] });

                var input = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("text_embed", batchedEmbeddings),
                    NamedOnnxValue.CreateFromTensor("latent", batchedSample),
                    NamedOnnxValue.CreateFromTensor("timestep", new DenseTensor<long>(new[] { timestep }, new[] { 1 }))
                };

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create model input");
                throw new InvalidOperationException("Failed to create model input", ex);
            }
        }

        private DenseTensor<float> ApplyGuidance(DenseTensor<float> noisePred, DenseTensor<float> noisePredText)
        {
            if (noisePred == null)
                throw new ArgumentNullException(nameof(noisePred));
            if (noisePredText == null)
                throw new ArgumentNullException(nameof(noisePredText));

            try
            {
                var noisePredArray = noisePred.ToArray();
                var noisePredTextArray = noisePredText.ToArray();

                if (noisePredArray.Length != noisePredTextArray.Length)
                {
                    throw new InvalidOperationException(
                        $"Noise prediction tensors have mismatched lengths: {noisePredArray.Length} vs {noisePredTextArray.Length}");
                }

                var guidedArray = new float[noisePred.Length];
                for (int i = 0; i < noisePred.Length; i++)
                {
                    guidedArray[i] = noisePredArray[i] + _guidanceScale * (noisePredTextArray[i] - noisePredArray[i]);
                }

                return TensorHelper.CreateTensor(guidedArray, noisePred.Dimensions.ToArray());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to apply guidance", ex);
            }
        }

        private void ValidateInputDimensions(DenseTensor<float> encoderHiddenStates, DenseTensor<float> sample)
        {
            if (encoderHiddenStates.Dimensions.Length != 3)
            {
                throw new ArgumentException(
                    $"Invalid encoder hidden states dimensions: {FormatDimensions(encoderHiddenStates.Dimensions)}. Expected 3 dimensions.");
            }

            if (sample.Dimensions.Length != 4)
            {
                throw new ArgumentException(
                    $"Invalid sample dimensions: {FormatDimensions(sample.Dimensions)}. Expected 4 dimensions.");
            }

            if (sample.Dimensions[0] != 1 || sample.Dimensions[1] != 4)
            {
                throw new ArgumentException(
                    $"Invalid sample shape: {FormatDimensions(sample.Dimensions)}. Expected [1, 4, H, W]");
            }
        }

        private void ValidateOutputShape(DenseTensor<float> output, int[] expectedShape)
        {
            var actualShape = output.Dimensions.ToArray();
            if (!actualShape.SequenceEqual(expectedShape))
            {
                throw new InvalidOperationException(
                    $"Unexpected output shape: {FormatDimensions(actualShape)}. Expected: {FormatDimensions(expectedShape)}");
            }
        }

        private static string FormatDimensions(ReadOnlySpan<int> dimensions)
        {
            return $"[{string.Join(",", dimensions.ToArray())}]";
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
