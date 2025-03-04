using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public class LMSDiscreteScheduler : SchedulerBase
    {
        private Tensor<float>? _sigmas;
        private List<int>? _timesteps;
        private readonly float[] _alphasCumprod;
        private readonly float[] _lmsCoeffs;
        private readonly Queue<DenseTensor<float>> _noisePredsQueue;
        private readonly int _order = 4;
        private float _initNoiseSigma;
        private readonly StableDiffusionConfig _config;

        public override Tensor<float> Sigmas
        {
            get => _sigmas ?? throw new InvalidOperationException("Sigmas not initialized");
            set => _sigmas = value;
        }

        public override List<int> Timesteps
        {
            get => _timesteps ?? throw new InvalidOperationException("Timesteps not initialized");
            set => _timesteps = value;
        }

        public override float InitNoiseSigma
        {
            get => _initNoiseSigma;
            set => _initNoiseSigma = value;
        }

        public LMSDiscreteScheduler(StableDiffusionConfig config)
        {
            _config = config;
            
            // Generate alpha cumprod
            var betasArray = NumericalArrayHelper.Linspace(0.00085, 0.012, 1000)
                .Select(x => (float)x).ToArray();
            _alphasCumprod = GetAlphasCumprod(betasArray);
            
            // Pre-calculate LMS coefficients
            _lmsCoeffs = GetLmsCoefficients(_order);
            _noisePredsQueue = new Queue<DenseTensor<float>>();
        }

        public override DenseTensor<float> Step(
            Tensor<float> modelOutput,
            int timestep,
            Tensor<float> sample,
            int order = 4)
        {
            if (order > _order)
            {
                throw new ArgumentException($"Order {order} greater than maximum {_order}");
            }

            var modelOutputDense = modelOutput as DenseTensor<float> ?? 
                throw new ArgumentException("Model output must be DenseTensor<float>");
            var sampleDense = sample as DenseTensor<float> ?? 
                throw new ArgumentException("Sample must be DenseTensor<float>");

            // Store noise predictions
            _noisePredsQueue.Enqueue(modelOutputDense);
            if (_noisePredsQueue.Count > order)
            {
                _noisePredsQueue.Dequeue();
            }

            var stepIndex = Timesteps.IndexOf(timestep);
            if (stepIndex == -1)
            {
                throw new ArgumentException($"Invalid timestep {timestep}");
            }

            var alphaProdt = _alphasCumprod[timestep];
            var betaProdt = 1 - alphaProdt;
            var currentSampleCoeff = (float)Math.Sqrt(alphaProdt);
            var currentNoiseSigma = (float)Math.Sqrt(betaProdt);

            // Get previous step value
            var prevStep = stepIndex < Timesteps.Count - 1 ? Timesteps[stepIndex + 1] : timestep;
            var alphaProdtPrev = _alphasCumprod[prevStep];
            var betaProdtPrev = 1 - alphaProdtPrev;
            var previousNoiseSigma = (float)Math.Sqrt(betaProdtPrev);

            var dimensions = sampleDense.Dimensions.ToArray();
            var dt = Math.Abs(previousNoiseSigma - currentNoiseSigma);

            if (_noisePredsQueue.Count < 1)
                throw new InvalidOperationException("Noise predictions queue is empty");

            // Multistep combining previous noise predictions
            var noisePreds = _noisePredsQueue.ToList();
            var stepMultiplier = _lmsCoeffs.Take(noisePreds.Count).ToArray();

            var noiseResult = new float[sampleDense.Length];

            for (int i = 0; i < sampleDense.Length; i++)
            {
                float noiseSum = 0;
                for (int j = 0; j < noisePreds.Count; j++)
                {
                    noiseSum += noisePreds[j].GetValue(i) * stepMultiplier[j];
                }
                noiseResult[i] = currentSampleCoeff * sampleDense.GetValue(i) - dt * noiseSum;
            }

            return new DenseTensor<float>(noiseResult, dimensions);
        }

        public override int[] SetTimesteps(int numInferenceSteps)
        {
            var timesteps = Enumerable.Range(0, numInferenceSteps)
                .Select(i => numInferenceSteps - 1 - i)
                .ToArray();

            Timesteps = timesteps.ToList();
            InitNoiseSigma = (float)Math.Sqrt(1f / (1f - _alphasCumprod[timesteps[0]]));
            _noisePredsQueue.Clear();
            return timesteps;
        }

        public override DenseTensor<float> ScaleInput(float[] latents, long timestep)
        {
            var stepIndex = Timesteps.IndexOf((int)timestep);
            if (stepIndex == -1)
                throw new ArgumentException($"Invalid timestep {timestep}");

            var sigma = _sigmas?.GetValue(stepIndex) ?? 
                throw new InvalidOperationException("Sigmas not initialized");

            var dimensions = new[] { 1, 4, _config.Height / 8, _config.Width / 8 };
            
            var scaledLatents = new float[latents.Length];
            for (int i = 0; i < latents.Length; i++)
            {
                var scale = (sigma * sigma + 1) * (float)Math.Sqrt(sigma * sigma + 1);
                scaledLatents[i] = latents[i] / scale;
            }

            return new DenseTensor<float>(scaledLatents, dimensions);
        }

        private float[] GetLmsCoefficients(int order)
        {
            var coeffs = new float[order];
            for (int i = 0; i < order; i++)
            {
                var prod = 1.0f;
                for (int j = 0; j < order; j++)
                {
                    if (i != j)
                    {
                        prod *= (i + 1) / (float)((i - j) + 1e-7);
                    }
                }
                coeffs[i] = prod;
            }
            return coeffs;
        }

        private float[] GetAlphasCumprod(float[] betas)
        {
            var alphas = betas.Select(beta => 1.0f - beta).ToArray();
            var alphasCumprod = new float[alphas.Length];
            alphasCumprod[0] = alphas[0];
            for (int i = 1; i < alphas.Length; i++)
            {
                alphasCumprod[i] = alphasCumprod[i - 1] * alphas[i];
            }
            return alphasCumprod;
        }

        private 
    }
}
