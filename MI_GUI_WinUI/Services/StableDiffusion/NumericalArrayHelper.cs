using System;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    internal static class NumericalArrayHelper
    {
        public static double[] Linspace(double start, double stop, int num)
        {
            if (num < 0)
                throw new ArgumentException("Number of samples, num, must be non-negative.");

            if (num == 0)
                return Array.Empty<double>();

            if (num == 1)
                return new[] { start };

            var step = (stop - start) / (num - 1);
            return Enumerable.Range(0, num)
                .Select(i => start + step * i)
                .ToArray();
        }

        public static double[] Arange(double start, double stop)
        {
            var count = (int)(stop - start);
            return Enumerable.Range(0, count)
                .Select(i => start + i)
                .ToArray();
        }
    }
}
