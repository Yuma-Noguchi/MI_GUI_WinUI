using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using Moq;
using MI_GUI_WinUI.Tests.TestUtils;

namespace MI_GUI_WinUI.Tests.Infrastructure
{
    [TestClass]
    public class TestSetupVerification : UnitTestBase
    {
        private string _testOutputPath;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            _testOutputPath = Path.Combine(TestDirectory, "TestOutput");
            Directory.CreateDirectory(_testOutputPath);
        }

        [TestCleanup]
        public override void CleanupTest()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
            base.CleanupTest();
        }

        [TestMethod]
        [SmokeTest]
        public void VerifyBasicSetup()
        {
            // Verify base configuration is working
            Assert.IsNotNull(ServiceProvider, "Service provider should be initialized");
            Assert.IsNotNull(TestDirectory, "Test directory should be initialized");
            Assert.IsTrue(Directory.Exists(_testOutputPath), "Test output directory should be created");
        }

        [TestMethod]
        [SmokeTest]
        public void VerifyMockServiceSetup()
        {
            // Verify mock services are initialized
            Assert.IsNotNull(MockLogger, "Logger mock should be initialized");
            Assert.IsNotNull(MockNavigationService, "Navigation service mock should be initialized");
            Assert.IsNotNull(MockActionService, "Action service mock should be initialized");
            Assert.IsNotNull(MockStableDiffusionService, "Stable diffusion service mock should be initialized");
        }

        [TestMethod]
        [DataDependentTest("TestFiles")]
        public void VerifyTestDataFiles()
        {
            // Test profile data
            var profilePath = Path.Combine(TestDirectory, "TestData", "Profiles", "sample_profile.json");
            Assert.IsTrue(File.Exists(profilePath), "Sample profile file not found");
            var profileJson = File.ReadAllText(profilePath);
            var profileData = JsonDocument.Parse(profileJson);
            Assert.IsNotNull(profileData);

            // Test action data
            var actionPath = Path.Combine(TestDirectory, "TestData", "Actions", "sample_action.json");
            Assert.IsTrue(File.Exists(actionPath), "Sample action file not found");
            var actionJson = File.ReadAllText(actionPath);
            var actionData = JsonDocument.Parse(actionJson);
            Assert.IsNotNull(actionData);

            // Test config
            var configPath = Path.Combine(TestDirectory, "TestData", "Config", "test_config.json");
            Assert.IsTrue(File.Exists(configPath), "Test config file not found");
            var configJson = File.ReadAllText(configPath);
            var configData = JsonDocument.Parse(configJson);
            Assert.IsNotNull(configData);

            // Test prompts
            var promptsPath = Path.Combine(TestDirectory, "TestData", "Prompts", "test_prompts.json");
            Assert.IsTrue(File.Exists(promptsPath), "Test prompts file not found");
            var promptsJson = File.ReadAllText(promptsPath);
            var promptsData = JsonDocument.Parse(promptsJson);
            Assert.IsNotNull(promptsData);
        }

        [TestMethod]
        [SmokeTest]
        public void VerifyTestUtilities()
        {
            // Test data generators
            var action = TestDataGenerators.CreateAction();
            Assert.IsNotNull(action);
            Assert.IsFalse(string.IsNullOrEmpty(action.Id));
            Assert.IsFalse(string.IsNullOrEmpty(action.Name));

            // Test image data
            var imageData = TestDataGenerators.CreateTestImage();
            Assert.IsNotNull(imageData);
            Assert.IsTrue(imageData.Length > 0);

            // Test prompts
            var prompts = TestDataGenerators.CreateTestPrompts().ToArray();
            Assert.IsTrue(prompts.Length > 0);
            Assert.IsFalse(string.IsNullOrEmpty(prompts[0]));
        }

        [TestMethod]
        [SmokeTest]
        public async Task VerifyAsyncTestSupport()
        {
            // Test file operations
            var testContent = "Test content";
            var filePath = Path.Combine(_testOutputPath, "test.txt");
            await File.WriteAllTextAsync(filePath, testContent);
            Assert.IsTrue(File.Exists(filePath));

            // Verify file content
            var content = await File.ReadAllTextAsync(filePath);
            Assert.AreEqual(testContent, content);

            // Test async exception handling
            await VerifyException<InvalidOperationException>(async () =>
            {
                throw new InvalidOperationException("Test exception");
            });
        }

        [TestMethod]
        [SmokeTest]
        public void VerifyLoggingSetup()
        {
            // Arrange
            var testMessage = "Test log message";

            // Act
            Logger.LogInformation(testMessage);

            // Assert
            MockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(testMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
        }

        [TestMethod]
        [RequiresPlatform("FileSystem")]
        public void VerifyFileSystemAccess()
        {
            // Create test directory
            var testDir = Path.Combine(_testOutputPath, "test_verify");
            Directory.CreateDirectory(testDir);
            Assert.IsTrue(Directory.Exists(testDir), "Should be able to create directories");

            // Create test file
            var testFile = Path.Combine(testDir, "test.txt");
            File.WriteAllText(testFile, "test");
            Assert.IsTrue(File.Exists(testFile), "Should be able to create files");

            // Read test file
            var content = File.ReadAllText(testFile);
            Assert.AreEqual("test", content, "Should be able to read files");

            // Delete test file
            File.Delete(testFile);
            Assert.IsFalse(File.Exists(testFile), "Should be able to delete files");

            // Delete test directory
            Directory.Delete(testDir);
            Assert.IsFalse(Directory.Exists(testDir), "Should be able to delete directories");
        }
    }
}