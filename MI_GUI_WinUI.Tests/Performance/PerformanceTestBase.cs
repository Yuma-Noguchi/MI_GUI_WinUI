using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Tests.TestUtils;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.Tests.Performance
{
    /// <summary>
    /// Base class for performance tests providing timing and metrics collection
    /// </summary>
    public abstract class PerformanceTestBase : TestBase
    {
        protected readonly ConcurrentDictionary<string, List<long>> Measurements = new();
        protected const int WarmupCount = 3;
        protected const int IterationCount = 10;

        protected struct PerformanceMetrics
        {
            public double AverageMs { get; set; }
            public double MedianMs { get; set; }
            public double P95Ms { get; set; }
            public double MinMs { get; set; }
            public double MaxMs { get; set; }
            public int SampleCount { get; set; }
        }

        /// <summary>
        /// Measures the execution time of an action
        /// </summary>
        protected async Task<long> MeasureExecutionTime(Func<Task> action)
        {
            var sw = Stopwatch.StartNew();
            await action();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// Runs a performance test with warmup and multiple iterations
        /// </summary>
        protected async Task RunPerformanceTest(string testName, Func<Task> action)
        {
            // Warmup
            for (int i = 0; i < WarmupCount; i++)
            {
                await action();
            }

            // Actual measurements
            var measurements = new List<long>();
            for (int i = 0; i < IterationCount; i++)
            {
                var elapsed = await MeasureExecutionTime(action);
                measurements.Add(elapsed);
            }

            Measurements.TryAdd(testName, measurements);
        }

        /// <summary>
        /// Calculates metrics from collected measurements
        /// </summary>
        protected PerformanceMetrics CalculateMetrics(string testName)
        {
            if (!Measurements.TryGetValue(testName, out var measurements))
            {
                throw new InvalidOperationException($"No measurements found for test: {testName}");
            }

            var sortedMeasurements = measurements.OrderBy(m => m).ToList();
            var p95Index = (int)Math.Ceiling(measurements.Count * 0.95) - 1;
            var medianIndex = measurements.Count / 2;

            return new PerformanceMetrics
            {
                AverageMs = measurements.Average(),
                MedianMs = sortedMeasurements[medianIndex],
                P95Ms = sortedMeasurements[p95Index],
                MinMs = sortedMeasurements.First(),
                MaxMs = sortedMeasurements.Last(),
                SampleCount = measurements.Count
            };
        }

        /// <summary>
        /// Asserts that performance metrics meet specified thresholds
        /// </summary>
        protected void AssertPerformance(string testName, double maxAverageMs, double maxP95Ms)
        {
            var metrics = CalculateMetrics(testName);

            Assert.IsTrue(
                metrics.AverageMs <= maxAverageMs,
                $"Average execution time ({metrics.AverageMs:F2}ms) exceeds threshold ({maxAverageMs}ms)");

            Assert.IsTrue(
                metrics.P95Ms <= maxP95Ms,
                $"P95 execution time ({metrics.P95Ms:F2}ms) exceeds threshold ({maxP95Ms}ms)");
        }

        /// <summary>
        /// Logs performance metrics
        /// </summary>
        protected void LogMetrics(string testName)
        {
            var metrics = CalculateMetrics(testName);
            
            Logger.LogInformation(
                "Performance Test Results for {TestName}:\n" +
                "  Average: {Average:F2}ms\n" +
                "  Median:  {Median:F2}ms\n" +
                "  P95:     {P95:F2}ms\n" +
                "  Min:     {Min:F2}ms\n" +
                "  Max:     {Max:F2}ms\n" +
                "  Samples: {SampleCount}",
                testName,
                metrics.AverageMs,
                metrics.MedianMs,
                metrics.P95Ms,
                metrics.MinMs,
                metrics.MaxMs,
                metrics.SampleCount
            );
        }

        /// <summary>
        /// Measures memory usage during test execution
        /// </summary>
        protected async Task<(long peakMemoryBytes, TimeSpan elapsed)> MeasureMemoryUsage(Func<Task> action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var startMemory = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();

            await action();

            sw.Stop();
            GC.Collect();
            
            var endMemory = GC.GetTotalMemory(true);
            var peakMemory = endMemory - startMemory;

            return (peakMemory, sw.Elapsed);
        }

        /// <summary>
        /// Asserts that memory usage is within acceptable limits
        /// </summary>
        protected void AssertMemoryUsage(long actualBytes, long maxBytes)
        {
            Assert.IsTrue(
                actualBytes <= maxBytes,
                $"Memory usage ({actualBytes / 1024.0 / 1024.0:F2}MB) exceeds threshold ({maxBytes / 1024.0 / 1024.0:F2}MB)"
            );
        }
    }
}