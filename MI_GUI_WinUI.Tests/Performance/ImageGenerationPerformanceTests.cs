using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.Tests.Performance
{
    [TestClass]
    public class ImageGenerationPerformanceTests : PerformanceTestBase
    {
        private IStableDiffusionService _sdService;
        private const int IMAGE_SIZE = 512;
        private const int MAX_AVERAGE_GENERATION_TIME_MS = 5000; // 5 seconds
        private const int MAX_P95_GENERATION_TIME_MS = 7000;     // 7 seconds
        private const long MAX_MEMORY_USAGE_BYTES = 1024L * 1024L * 1024L; // 1GB

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();

            // Get real service implementation for performance testing
            _sdService = GetService<IStableDiffusionService>();
            await _sdService.Initialize(useGpu: true); // Was: useGpu: true
        }

        [TestMethod]
        public async Task SingleImageGeneration_Performance()
        {
            // Arrange
            var prompt = TestDataGenerators.CreateTestPrompts(1).First();
            var testName = "SingleImageGeneration";

            // Act
            await RunPerformanceTest(testName, async () =>
            {
                var images = await _sdService.GenerateImages(prompt, 1);
                Assert.IsTrue(images.Length > 0, "Image generation failed");
            });

            // Assert
            AssertPerformance(testName, MAX_AVERAGE_GENERATION_TIME_MS, MAX_P95_GENERATION_TIME_MS);
            LogMetrics(testName);
        }

        [TestMethod]
        public async Task BatchImageGeneration_Performance()
        {
            // Arrange
            var prompt = TestDataGenerators.CreateTestPrompts(1).First();
            var testName = "BatchImageGeneration";
            const int batchSize = 4;

            // Act
            await RunPerformanceTest(testName, async () =>
            {
                var images = await _sdService.GenerateImages(prompt, batchSize);
                Assert.AreEqual(batchSize, images.Length, "Incorrect number of images generated");
            });

            // Assert
            // Expect batch generation to take longer but be more efficient per image
            AssertPerformance(testName, 
                MAX_AVERAGE_GENERATION_TIME_MS * 2, // Allow more time for batch
                MAX_P95_GENERATION_TIME_MS * 2);
            LogMetrics(testName);
        }

        [TestMethod]
        public async Task ImageGeneration_MemoryUsage()
        {
            // Arrange
            var prompt = TestDataGenerators.CreateTestPrompts(1).First();

            // Act
            var (peakMemory, elapsed) = await MeasureMemoryUsage(async () =>
            {
                var images = await _sdService.GenerateImages(prompt, 1);
                Assert.IsTrue(images.Length > 0, "Image generation failed");
            });

            // Assert
            AssertMemoryUsage(peakMemory, MAX_MEMORY_USAGE_BYTES);
            
            Logger.LogInformation(
                "Memory Usage Test Results:\n" +
                "  Peak Memory: {Memory:F2}MB\n" +
                "  Elapsed Time: {Time:F2}s",
                peakMemory / 1024.0 / 1024.0,
                elapsed.TotalSeconds);
        }

        [TestMethod]
        public async Task ConcurrentImageGeneration_Performance()
        {
            // Arrange
            var prompts = TestDataGenerators.CreateTestPrompts(4).ToList();
            var testName = "ConcurrentImageGeneration";

            // Act
            await RunPerformanceTest(testName, async () =>
            {
                var tasks = prompts.Select(prompt => _sdService.GenerateImages(prompt, 1));
                var results = await Task.WhenAll(tasks);

                Assert.AreEqual(prompts.Count, results.Length, "Not all images were generated");
                Assert.IsTrue(results.All(r => r.Length > 0), "Some image generations failed");
            });

            // Assert
            // Concurrent generation might be slower due to resource contention
            AssertPerformance(testName,
                MAX_AVERAGE_GENERATION_TIME_MS * 1.5, // Allow 50% more time
                MAX_P95_GENERATION_TIME_MS * 1.5);
            LogMetrics(testName);
        }

        [TestMethod]
        public async Task ModelInitialization_Performance()
        {
            // Arrange
            var testName = "ModelInitialization";

            // Act
            await RunPerformanceTest(testName, async () =>
            {
                var newService = GetService<IStableDiffusionService>();
                await newService.Initialize(useGpu: false);
                Assert.IsTrue(newService.IsInitialized, "Model initialization failed");
            });

            // Assert
            AssertPerformance(testName, 2000, 3000); // Expect initialization within 2-3 seconds
            LogMetrics(testName);
        }

        [TestMethod]
        public async Task CpuVsGpuPerformance_Comparison()
        {
            // Arrange
            var prompt = TestDataGenerators.CreateTestPrompts(1).First();

            // GPU Test
            var gpuService = GetService<IStableDiffusionService>();
            await gpuService.Initialize(useGpu: true);
            await RunPerformanceTest("GPU_Generation", async () =>
            {
                var images = await gpuService.GenerateImages(prompt, 1);
                Assert.IsTrue(images.Length > 0);
            });

            // CPU Test
            var cpuService = GetService<IStableDiffusionService>();
            await cpuService.Initialize(useGpu: false);
            await RunPerformanceTest("CPU_Generation", async () =>
            {
                var images = await cpuService.GenerateImages(prompt, 1);
                Assert.IsTrue(images.Length > 0);
            });

            // Log comparison
            LogMetrics("GPU_Generation");
            LogMetrics("CPU_Generation");

            // Compare results
            var gpuMetrics = CalculateMetrics("GPU_Generation");
            var cpuMetrics = CalculateMetrics("CPU_Generation");

            Logger.LogInformation(
                "Performance Comparison:\n" +
                "  GPU Average: {GPUAvg:F2}ms\n" +
                "  CPU Average: {CPUAvg:F2}ms\n" +
                "  Speedup Factor: {Speedup:F2}x",
                gpuMetrics.AverageMs,
                cpuMetrics.AverageMs,
                cpuMetrics.AverageMs / gpuMetrics.AverageMs);
        }
    }
}