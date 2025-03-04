using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    public abstract class SchedulerBase
    {
        protected List<float> _alphasCumulativeProducts;

        public abstract Tensor<float> Sigmas { get; set; }
        public abstract List<int> Timesteps { get; set; }
        public abstract float InitNoiseSigma { get; set; }

        protected SchedulerBase()
        {
            _alphasCumulativeProducts = new List<float>();
        }

        public abstract DenseTensor<float> Step(
            Tensor<float> modelOutput,
            int timestep,
            Tensor<float> sample,
            int order = 4);

        public abstract DenseTensor<float> ScaleInput(float[] latents, long timestep);

        public abstract int[] SetTimesteps(int numInferenceSteps);

        protected float[] GetBetaSchedule(string schedule, int trainTimesteps, float betaStart = 0.00085f, float betaEnd = 0.012f)
        {
            switch (schedule)
            {
                case "linear":
                    return Enumerable.Range(0, trainTimesteps)
                        .Select(i => betaStart + (betaEnd - betaStart) * i / (trainTimesteps - 1))
                        .ToArray();

                case "scaled_linear":
                    var start = (float)Math.Sqrt(betaStart);
                    var end = (float)Math.Sqrt(betaEnd);
                    return Enumerable.Range(0, trainTimesteps)
                        .Select(i => start + (end - start) * i / (trainTimesteps - 1))
                        .Select(x => x * x)
                        .ToArray();

                default:
                    throw new ArgumentException($"Unknown beta schedule: {schedule}");
            }
        }

        protected List<float> GetAlphasCumulativeProducts(float[] betas)
        {
            var alphas = betas.Select(beta => 1.0f - beta);
            return alphas
                .Select((alpha, i) => alphas.Take(i + 1).Aggregate((a, b) => a * b))
                .ToList();
        }

        protected float[] Interpolate(double[] timesteps, double[] range, List<float> sigmas)
        {
            var interpolatedSigmas = new float[timesteps.Length];
            for (var i = 0; i < timesteps.Length; i++)
            {
                var rangeIndex = timesteps[i];
                var rangeLow = (int)Math.Floor(rangeIndex);
                var rangeHigh = (int)Math.Ceiling(rangeIndex);
                var weight = rangeIndex - rangeLow;

                if (rangeLow == rangeHigh)
                {
                    interpolatedSigmas[i] = sigmas[rangeLow];
                }
                else
                {
                    interpolatedSigmas[i] = (float)((1 - weight) * sigmas[rangeLow] + weight * sigmas[rangeHigh]);
                }
            }
            return interpolatedSigmas;
        }
    }
}
