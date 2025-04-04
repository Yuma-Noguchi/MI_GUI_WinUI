using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class LoggingServiceIntegrationTests : IntegrationTestBase
    {
        private ILoggingService _loggingService;
        private string _logFilePath;
        private TestableLoggingService _testableService;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            
            // Set up log file path
            _logFilePath = Path.Combine(TestDataPath, "Logs", "IntegrationTestLog.log");
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
            
            // Clear existing log file if present
            if (File.Exists(_logFilePath))
            {
                File.Delete(_logFilePath);
            }
            
            // Create testable logging service
            _testableService = new TestableLoggingService(
                GetRequiredService<ILogger<TestableLoggingService>>(),
                _logFilePath
            );
            _loggingService = _testableService;
        }

        protected override void ConfigureIntegrationServices(IServiceCollection services)
        {
            base.ConfigureIntegrationServices(services);
            
            // Any additional service configuration for testing can go here
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task LoggingLifecycle_CompleteWorkflow()
        {
            // Step 1: Write various log messages
            _loggingService.LogInformation("Integration test information message");
            _loggingService.LogError(new Exception("Test exception"), "Integration test error message");
            
            // Allow time for async file operations
            await Task.Delay(400);
            
            // Step 2: Verify logs were written properly
            Assert.IsTrue(File.Exists(_logFilePath), "Log file should exist");
            string logContent = await File.ReadAllTextAsync(_logFilePath);
            
            Assert.IsTrue(logContent.Contains("Integration test information message"), "Log should contain info message");
            Assert.IsTrue(logContent.Contains("Integration test error message"), "Log should contain error message");
            Assert.IsTrue(logContent.Contains("Test exception"), "Log should contain exception message");
            
            // Step 3: Verify logs can be retrieved
            string retrievedLogs = await _testableService.GetLogsAsync();
            Assert.AreEqual(logContent, retrievedLogs, "Retrieved logs should match file content");
            
            // Step 4: Clear logs
            await _testableService.ClearLogsAsync();
            
            // Allow time for async file operations
            await Task.Delay(100);
            
            // Step 5: Verify logs were cleared (except for the "log cleared" message)
            string clearedLogContent = await File.ReadAllTextAsync(_logFilePath);
            Assert.IsFalse(clearedLogContent.Contains("Integration test information message"), 
                "Cleared log should not contain previous messages");
            Assert.IsTrue(clearedLogContent.Contains("Log file cleared"), 
                "Log should contain the cleared confirmation message");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task LargeLogFile_CanBeReadAndCleared()
        {
            // Arrange - Create a large log file
            for (int i = 0; i < 100; i++)
            {
                _loggingService.LogInformation($"Large log message {i}");
            }
            
            // Allow time for async file operations
            await Task.Delay(300);
            
            // Act - Get logs
            string retrievedLogs = await _testableService.GetLogsAsync();
            
            // Assert
            Assert.IsTrue(retrievedLogs.Length > 9000, "Retrieved log should be larger than 9KB");
            
            // Verify some messages at different positions
            Assert.IsTrue(retrievedLogs.Contains("Large log message 0"), "Log should contain first message");
            Assert.IsTrue(retrievedLogs.Contains("Large log message 50"), "Log should contain middle message");
            Assert.IsTrue(retrievedLogs.Contains("Large log message 99"), "Log should contain last message");
            
            // Act - Clear logs
            await _testableService.ClearLogsAsync();
            
            // Allow time for async file operations
            await Task.Delay(100);
            
            // Assert - File should be much smaller and not contain old messages
            string clearedLogs = await File.ReadAllTextAsync(_logFilePath);
            Assert.IsFalse(clearedLogs.Contains("Large log message 50"), 
                "Cleared log should not contain previous messages");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task FileSystem_CreatesMissingDirectories()
        {
            // Arrange
            string deepLogPath = Path.Combine(TestDataPath, "DeepLogs", "Level1", "Level2", "deep_test.log");
            
            // Ensure directory doesn't exist
            if (Directory.Exists(Path.GetDirectoryName(deepLogPath)))
            {
                Directory.Delete(Path.GetDirectoryName(deepLogPath), true);
            }
            
            // Act
            var deepLogger = new TestableLoggingService(
                GetRequiredService<ILogger<TestableLoggingService>>(),
                deepLogPath
            );
            
            deepLogger.LogInformation("Deep log test message");
            
            // Allow time for async file operations
            await Task.Delay(100);
            
            // Assert
            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(deepLogPath)), 
                "Logger should create missing directories");
            Assert.IsTrue(File.Exists(deepLogPath), 
                "Log file should be created in nested directories");
            
            string logContent = await File.ReadAllTextAsync(deepLogPath);
            Assert.IsTrue(logContent.Contains("Deep log test message"), 
                "Log file should contain the message");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task LogFilePermissions_AllowsReadWrite()
        {
            // Arrange
            _loggingService.LogInformation("Permission test message");
            
            // Allow time for async file operations
            await Task.Delay(100);
            
            // Act - Test file permissions by re-opening the file for reading and writing
            using (var fileStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                // Assert - Read content
                string content = reader.ReadToEnd();
                Assert.IsTrue(content.Contains("Permission test message"), 
                    "Should be able to read log content");
                
                // Test write access
                var writer = new StreamWriter(fileStream);
                writer.WriteLine("Direct file write test");
                writer.Flush();
            }
            
            // Verify both the original and new content
            string updatedContent = await File.ReadAllTextAsync(_logFilePath);
            Assert.IsTrue(updatedContent.Contains("Permission test message"), 
                "File should still contain original message");
            Assert.IsTrue(updatedContent.Contains("Direct file write test"), 
                "File should contain directly written content");
        }
        
        [TestMethod]
        [TestCategory("Integration")]
        public async Task LogFileRotation_SimulateLogGrowthScenario()
        {
            // This test demonstrates how to test log file rotation if implemented
            // Currently, LoggingService doesn't implement log rotation, but this shows
            // how we would test it if it were implemented
            
            // Arrange - Create a large log file that would trigger rotation in a real system
            string baseLogMessage = new string('X', 1000); // 1KB message
            
            // Write 5MB of logs
            for (int i = 0; i < 5000; i++)
            {
                _loggingService.LogInformation($"Log rotation test message {i}: {baseLogMessage}");
            }
            
            // Allow time for async file operations
            await Task.Delay(1000);
            
            // Assert
            Assert.IsTrue(File.Exists(_logFilePath), "Log file should exist");
            
            // Add this verification if log rotation is implemented:
            /*
            string rotatedFilePath = _logFilePath + ".1";
            Assert.IsTrue(File.Exists(rotatedFilePath), "Rotated log file should exist");
            */
            
            // Verify file size is large
            var fileInfo = new FileInfo(_logFilePath);
            Assert.IsTrue(fileInfo.Length > 1000000, "Log file should be larger than 1MB");
        }
    }
}