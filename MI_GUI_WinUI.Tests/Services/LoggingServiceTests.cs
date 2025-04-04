using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Tests.TestUtils;
using MI_GUI_WinUI.Tests.TestUtils.TestServices;
using Moq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Tests.Services
{
    [TestClass]
    public class LoggingServiceTests : UnitTestBase
    {
        private ILoggingService _loggingService;
        private string _logFilePath;
        private Mock<ILogger<TestableLoggingService>> _mockLogger;
        private TestableLoggingService _testableLoggingService;

        [TestInitialize]
        public override async Task InitializeTest()
        {
            await base.InitializeTest();
            _logFilePath = Path.Combine(TestDirectory, "Logs", "TestLog.log");
            
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            else
            {
                // Clean up any existing files to start fresh
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
            }
            
            _mockLogger = new Mock<ILogger<TestableLoggingService>>();
            
            // Use the TestableLoggingService instead of the real LoggingService
            _testableLoggingService = new TestableLoggingService(_mockLogger.Object, _logFilePath);
            _loggingService = _testableLoggingService;
        }

        [TestMethod]
        public async Task LogInformation_WritesToFile()
        {
            // Arrange
            string testMessage = "Test information message";
            
            // Act
            _loggingService.LogInformation(testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(logContent.Contains("[INFO]"), "Log level should be INFO");
            Assert.IsTrue(logContent.Contains(testMessage), "Log should contain the message");
            
            // Verify in-memory entries
            var entries = _testableLoggingService.GetLogEntries();
            Assert.IsTrue(entries.Count > 0, "Should have at least one log entry");
            Assert.IsTrue(_testableLoggingService.ContainsLogEntry("INFO", testMessage), 
                "Should contain INFO entry with the test message");
        }

        [TestMethod]
        public async Task LogError_IncludesExceptionDetails()
        {
            // Arrange
            string testMessage = "Test error message";
            var testException = new InvalidOperationException("Test exception");
            
            // Act
            _loggingService.LogError(testException, testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(logContent.Contains("[ERROR]"), "Log level should be ERROR");
            Assert.IsTrue(logContent.Contains(testMessage), "Log should contain the message");
            Assert.IsTrue(logContent.Contains("InvalidOperationException"), "Log should contain exception type");
            Assert.IsTrue(logContent.Contains("Test exception"), "Log should contain exception message");
            
            // Verify in-memory entries
            Assert.IsTrue(_testableLoggingService.ContainsLogEntry("ERROR", testMessage), 
                "Should contain ERROR entry with the test message");
        }
        
        [TestMethod]
        public async Task LogWarning_WritesToFile()
        {
            // Arrange
            string testMessage = "Test warning message";
            
            // Act
            _loggingService.LogWarning(testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(logContent.Contains("[WARN]"), "Log level should be WARN");
            Assert.IsTrue(logContent.Contains(testMessage), "Log should contain the message");
            
            // Verify in-memory entries
            Assert.IsTrue(_testableLoggingService.ContainsLogEntry("WARN", testMessage), 
                "Should contain WARN entry with the test message");
        }
        
        [TestMethod]
        public async Task LogDebug_WritesToFileWhenEnabled()
        {
            // Arrange
            string testMessage = "Test debug message";
            
            // Act
            _loggingService.LogDebug(testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(logContent.Contains("[DEBUG]"), "Log level should be DEBUG");
            Assert.IsTrue(logContent.Contains(testMessage), "Log should contain the message");
            
            // Verify in-memory entries
            Assert.IsTrue(_testableLoggingService.ContainsLogEntry("DEBUG", testMessage), 
                "Should contain DEBUG entry with the test message");
        }
        
        [TestMethod]
        public async Task LogDebug_DoesNotWriteWhenDisabled()
        {
            // Arrange
            string testMessage = "Test debug message with debug disabled";
            var disabledDebugLogger = new TestableLoggingService(_mockLogger.Object, 
                Path.Combine(TestDirectory, "Logs", "DisabledDebug.log"), 
                includeDebugLogs: false);
            
            // Act
            disabledDebugLogger.LogDebug(testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await disabledDebugLogger.GetLogsAsync();
            Assert.IsFalse(logContent.Contains(testMessage), "Log should not contain debug message when disabled");
            
            // Verify in-memory entries
            var entries = disabledDebugLogger.GetLogEntries();
            Assert.AreEqual(0, entries.Count, "Should have no log entries when debug is disabled");
        }
        
        [TestMethod]
        public async Task LogCritical_IncludesExceptionDetails()
        {
            // Arrange
            string testMessage = "Test critical message";
            var testException = new ApplicationException("Critical test exception");
            
            // Act
            _loggingService.LogCritical(testException, testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(logContent.Contains("[CRITICAL]"), "Log level should be CRITICAL");
            Assert.IsTrue(logContent.Contains(testMessage), "Log should contain the message");
            Assert.IsTrue(logContent.Contains("ApplicationException"), "Log should contain exception type");
            Assert.IsTrue(logContent.Contains("Critical test exception"), "Log should contain exception message");
            
            // Verify in-memory entries
            Assert.IsTrue(_testableLoggingService.ContainsLogEntry("CRITICAL", testMessage), 
                "Should contain CRITICAL entry with the test message");
        }
        
        [TestMethod]
        public async Task GetLogsAsync_RetrievesLogContent()
        {
            // Arrange
            _loggingService.LogInformation("Test message 1");
            _loggingService.LogWarning("Test message 2");
            
            // Allow async operations to complete
            await Task.Delay(100);
            
            // Act
            string logs = await _testableLoggingService.GetLogsAsync();
            
            // Assert
            Assert.IsTrue(logs.Contains("Test message 1"), "Logs should contain first message");
            Assert.IsTrue(logs.Contains("Test message 2"), "Logs should contain second message");
        }
        
        [TestMethod]
        public async Task ClearLogsAsync_RemovesAllLogs()
        {
            // Arrange
            _loggingService.LogInformation("Test message before clear");
            await Task.Delay(100);
            
            string beforeClear = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(beforeClear.Contains("Test message before clear"), "Setup verification: Log should contain message");
            
            // Act
            await _testableLoggingService.ClearLogsAsync();
            await Task.Delay(100);
            
            // Assert
            string afterClear = await _testableLoggingService.GetLogsAsync();
            Assert.IsFalse(afterClear.Contains("Test message before clear"), "Logs should be cleared");
            
            // Verify it added a log cleared message
            Assert.IsTrue(_testableLoggingService.ContainsLogEntry("INFO", "Log file cleared"), 
                "Should log that the file was cleared");
        }
        
        [TestMethod]
        public async Task MultipleLogEntries_AppendedCorrectly()
        {
            // Arrange
            string[] messages = new string[]
            {
                "First log message",
                "Second log message",
                "Third log message"
            };
            
            // Act
            foreach (var message in messages)
            {
                _loggingService.LogInformation(message);
            }
            
            // Allow async operations to complete
            await Task.Delay(200);
            
            // Assert
            string logs = await _testableLoggingService.GetLogsAsync();
            
            foreach (var message in messages)
            {
                Assert.IsTrue(logs.Contains(message), $"Logs should contain message: {message}");
            }
            
            // Verify log entries are in correct order
            var entries = _testableLoggingService.GetLogEntries();
            Assert.AreEqual(3, entries.Count, "Should have 3 log entries");
            Assert.AreEqual("First log message", entries[0].Message);
            Assert.AreEqual("Second log message", entries[1].Message);
            Assert.AreEqual("Third log message", entries[2].Message);
        }
        
        [TestMethod]
        public async Task LogFormatting_IncludesTimestampAndSeparator()
        {
            // Arrange
            string testMessage = "Test format message";
            
            // Act
            _loggingService.LogInformation(testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            
            // Check timestamp format using regex - should match [yyyy-MM-dd HH:mm:ss.fff]
            var timestampPattern = @"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]";
            Assert.IsTrue(Regex.IsMatch(logContent, timestampPattern), "Log should contain properly formatted timestamp");
            
            // Check separator
            Assert.IsTrue(logContent.Contains("----------------------------------------"), 
                "Log should contain separator");
        }
        
        [TestMethod]
        public async Task NestedExceptionLogging_IncludesInnerExceptionDetails()
        {
            // Arrange
            var innerException = new ArgumentException("Inner exception message");
            var outerException = new InvalidOperationException("Outer exception message", innerException);
            string testMessage = "Test nested exceptions";
            
            // Act
            _loggingService.LogError(outerException, testMessage);
            
            // Allow async operation to complete
            await Task.Delay(100);
            
            // Assert
            string logContent = await _testableLoggingService.GetLogsAsync();
            Assert.IsTrue(logContent.Contains("Inner Exception: Inner exception message"), 
                "Log should contain inner exception details");
        }
    }
}