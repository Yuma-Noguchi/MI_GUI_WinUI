using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Extensions.Configuration;
using MI_GUI_WinUI.Services.Interfaces;

namespace MI_GUI_WinUI.Services
{
    /// <summary>
    /// Implementation of ILoggingService for file-based logging
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly string _logFolder;
        private readonly string _logFileName;
        private readonly object _logLock = new object();
        private readonly bool _includeDebugLogs;

        public LoggingService()
        {
            _logFolder = "Logs";
            _logFileName = "MotionInput_Configuration.log";
            _includeDebugLogs = false;
        }

        private async Task LogAsync(string level, string message, Exception? exception = null)
        {
            if (level == "DEBUG" && !_includeDebugLogs)
                return;

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var logFolder = await localFolder.CreateFolderAsync(_logFolder, CreationCollisionOption.OpenIfExists);
                var logFile = await logFolder.CreateFileAsync(_logFileName, CreationCollisionOption.OpenIfExists);

                string logMessage;
                lock (_logLock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
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

                await FileIO.AppendTextAsync(logFile, logMessage);
            }
            catch (Exception ex)
            {
                // If logging fails, try to write to a fallback location
                try
                {
                    var fallbackPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MotionInput",
                        "fallback.log"
                    );
                    File.AppendAllText(fallbackPath, $"Logging failed: {ex.Message}\nOriginal message: {message}\n");
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
                var localFolder = ApplicationData.Current.LocalFolder;
                var logFolder = await localFolder.CreateFolderAsync(_logFolder, CreationCollisionOption.OpenIfExists);
                var logFile = await logFolder.CreateFileAsync(_logFileName, CreationCollisionOption.OpenIfExists);

                return await FileIO.ReadTextAsync(logFile);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error reading logs");
                return "Error reading logs. Check the application event log for details.";
            }
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var logFolder = await localFolder.CreateFolderAsync(_logFolder, CreationCollisionOption.OpenIfExists);
                var logFile = await logFolder.CreateFileAsync(_logFileName, CreationCollisionOption.ReplaceExisting);
                
                LogInformation("Log file cleared");
                await FileIO.WriteTextAsync(logFile, string.Empty);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error clearing logs");
            }
        }
    }
}