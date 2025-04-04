using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MI_GUI_WinUI.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MI_GUI_WinUI.Tests.TestUtils.TestServices
{
    /// <summary>
    /// A testable version of LoggingService that doesn't rely on Windows.Storage APIs
    /// </summary>
    public class TestableLoggingService : ILoggingService
    {
        private readonly ILogger<TestableLoggingService> _logger;
        private readonly string _logFilePath;
        private readonly object _logLock = new object();
        private readonly bool _includeDebugLogs;
        
        // Store log entries in memory for verification in tests
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        
        public TestableLoggingService(ILogger<TestableLoggingService> logger, string logFilePath = null, bool includeDebugLogs = true)
        {
            _logger = logger;
            _includeDebugLogs = includeDebugLogs;
            
            if (string.IsNullOrEmpty(logFilePath))
            {
                var logsDir = Path.Combine(Path.GetTempPath(), "MI_GUI_WinUI_Tests", "Logs");
                Directory.CreateDirectory(logsDir);
                _logFilePath = Path.Combine(logsDir, "TestLog.log");
            }
            else
            {
                _logFilePath = logFilePath;
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
        
        private async Task LogAsync(string level, string message, Exception exception = null)
        {
            if (level == "DEBUG" && !_includeDebugLogs)
                return;
                
            try
            {
                var logEntry = new LogEntry
                {
                    Level = level,
                    Message = message,
                    Exception = exception,
                    Timestamp = DateTime.Now
                };
                
                _logEntries.Add(logEntry);
                
                string logMessage;
                lock (_logLock)
                {
                    var timestamp = logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    logMessage = $"[{timestamp}] [{level}] {message}";
            
                    if (exception != null)
                    {
                        logMessage += $"\nException: {exception.GetType().Name}\nMessage: {exception.Message}\nStack Trace:\n{exception.StackTrace}";
                        if (exception.InnerException != null)
                        {
                            logMessage += $"\nInner Exception: {exception.InnerException.Message}";
                        }
                    }

                    logMessage += "\n----------------------------------------\n";
                }

                await File.AppendAllTextAsync(_logFilePath, logMessage);
                
                // Also log to the ILogger for test visibility
                switch (level)
                {
                    case "INFO":
                        _logger.LogInformation(message);
                        break;
                    case "ERROR":
                        _logger.LogError(exception, message);
                        break;
                    case "WARN":
                        _logger.LogWarning(message);
                        break;
                    case "DEBUG":
                        _logger.LogDebug(message);
                        break;
                    case "CRITICAL":
                        _logger.LogCritical(exception, message);
                        break;
                }
            }
            catch (Exception ex)
            {
                // If logging fails, try to write to a fallback location
                try
                {
                    var fallbackPath = Path.Combine(
                        Path.GetTempPath(),
                        "MotionInput",
                        "fallback.log"
                    );
                    await File.AppendAllTextAsync(fallbackPath, $"Logging failed: {ex.Message}\nOriginal message: {message}\n");
                }
                catch
                {
                    // Silently fail if all logging attempts fail to prevent application disruption
                }
            }
        }
        
        public void LogInformation(string message)
        {
            _ = LogAsync("INFO", message);
        }

        public void LogError(Exception ex, string message)
        {
            _ = LogAsync("ERROR", message, ex);
        }

        public void LogWarning(string message)
        {
            _ = LogAsync("WARN", message);
        }

        public void LogDebug(string message)
        {
            _ = LogAsync("DEBUG", message);
        }

        public void LogCritical(Exception ex, string message)
        {
            _ = LogAsync("CRITICAL", message, ex);
        }
        
        public async Task<string> GetLogsAsync()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    return await File.ReadAllTextAsync(_logFilePath);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error reading logs");
                return "Error reading logs.";
            }
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    await File.WriteAllTextAsync(_logFilePath, string.Empty);
                }
                
                _logEntries.Clear();
                LogInformation("Log file cleared");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error clearing logs");
            }
        }
        
        // Test helper methods
        
        public string GetLogFilePath()
        {
            return _logFilePath;
        }
        
        public IReadOnlyList<LogEntry> GetLogEntries()
        {
            return _logEntries.AsReadOnly();
        }
        
        public bool ContainsLogEntry(string level, string messageFragment)
        {
            return _logEntries.Exists(entry => 
                string.Equals(entry.Level, level, StringComparison.OrdinalIgnoreCase) && 
                entry.Message.Contains(messageFragment));
        }
        
        public class LogEntry
        {
            public string Level { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}