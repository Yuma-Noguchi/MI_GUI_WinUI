using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Base class for integration tests using real service implementations
    /// </summary>
    public abstract class IntegrationTestBase : TestBase
    {
        protected string TestDataPath { get; private set; }
        protected string TestAssetsPath { get; private set; }

        public override async Task InitializeTest()
        {
            await base.InitializeTest();

            // Set up test paths
            TestDataPath = Path.Combine(TestDirectory, "Data");
            TestAssetsPath = Path.Combine(TestDirectory, "Assets");

            // Create test directories
            Directory.CreateDirectory(TestDataPath);
            Directory.CreateDirectory(TestAssetsPath);

            // Perform any additional setup
            await SetupTestData();
        }

        public override void CleanupTest()
        {
            // Cleanup any resources before base cleanup
            CleanupTestData();
            base.CleanupTest();
        }

        /// <summary>
        /// Configures services with real implementations
        /// </summary>
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // Register real service implementations
            services.AddSingleton<IActionService>(sp => 
                new ActionService(sp.GetRequiredService<ILogger<ActionService>>())
            );

            // For StableDiffusionService, we use CPU mode in tests
            services.AddSingleton<IStableDiffusionService>(sp => 
            {
                var service = new StableDiffusionService(
                    sp.GetRequiredService<ILogger<StableDiffusionService>>());
                service.Initialize(useGpu: false).Wait();
                return service;
            });

            // Add other real services
            ConfigureIntegrationServices(services);
        }

        /// <summary>
        /// Can be overridden by derived classes to configure additional services
        /// </summary>
        protected virtual void ConfigureIntegrationServices(IServiceCollection services)
        {
        }

        /// <summary>
        /// Can be overridden by derived classes to set up test data
        /// </summary>
        protected virtual Task SetupTestData()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Can be overridden by derived classes to clean up test data
        /// </summary>
        protected virtual void CleanupTestData()
        {
        }

        /// <summary>
        /// Creates test profile data
        /// </summary>
        protected async Task CreateTestProfile(string name, string content)
        {
            var profilePath = Path.Combine(TestDataPath, $"{name}.json");
            await File.WriteAllTextAsync(profilePath, content);
        }

        /// <summary>
        /// Creates test action data
        /// </summary>
        protected async Task CreateTestAction(string name, string content)
        {
            var actionPath = Path.Combine(TestDataPath, "actions", $"{name}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(actionPath));
            await File.WriteAllTextAsync(actionPath, content);
        }

        /// <summary>
        /// Creates a test image file
        /// </summary>
        protected async Task<string> CreateTestImage(string name, byte[] imageData)
        {
            var imagePath = Path.Combine(TestAssetsPath, name);
            await File.WriteAllBytesAsync(imagePath, imageData);
            return imagePath;
        }

        /// <summary>
        /// Waits for a condition to be true with timeout
        /// </summary>
        protected async Task WaitForCondition(Func<bool> condition, TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;

            while (!condition() && DateTime.UtcNow - startTime < timeout)
            {
                await Task.Delay(100);
            }

            if (!condition())
            {
                throw new TimeoutException($"Condition not met within {timeout.Value.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Verifies file existence and content
        /// </summary>
        protected async Task VerifyFileContent(string path, string expectedContent)
        {
            Assert.IsTrue(File.Exists(path), $"File does not exist: {path}");
            var actualContent = await File.ReadAllTextAsync(path);
            Assert.AreEqual(expectedContent, actualContent);
        }

        /// <summary>
        /// Gets a service of type T from the service provider
        /// Throws if service is not registered
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            var service = ServiceProvider.GetService<T>();
            Assert.IsNotNull(service, $"Service of type {typeof(T).Name} is not registered");
            return service;
        }
    }
}