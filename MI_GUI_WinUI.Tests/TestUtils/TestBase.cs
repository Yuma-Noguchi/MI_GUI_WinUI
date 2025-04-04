using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MI_GUI_WinUI.Tests.TestUtils
{
    /// <summary>
    /// Base class for all tests providing common functionality
    /// </summary>
    public abstract class TestBase
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        protected string TestDirectory { get; private set; }
        protected ILogger Logger { get; private set; }

        [TestInitialize]
        public virtual Task InitializeTest()
        {
            // Create a new service collection
            var services = new ServiceCollection();

            // Configure services
            ConfigureServices(services);

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();

            // Set up test directory
            TestDirectory = TestHelpers.CreateTestDirectory();

            TestDataInitializer.InitializeTestData(TestDirectory);

            // Get logger
            Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();

            return Task.CompletedTask;
        }

        [TestCleanup]
        public virtual void CleanupTest()
        {
            // Cleanup test directory
            TestHelpers.CleanupTestDirectory(TestDirectory);

            // Dispose service provider
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Configure services for the test
        /// Can be overridden by derived classes to add additional services
        /// </summary>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddDebug();
            });
        }

        /// <summary>
        /// Helper method to get a service from the provider
        /// </summary>
        protected T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Creates a test file with the specified content
        /// </summary>
        protected async Task<string> CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(TestDirectory, fileName);
            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Verifies that an exception of type T is thrown
        /// </summary>
        protected async Task VerifyException<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
                Assert.Fail($"Expected {typeof(T).Name} was not thrown");
            }
            catch (T)
            {
                // Expected exception was thrown
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected {typeof(T).Name} but got {ex.GetType().Name}");
            }
        }
    }
}