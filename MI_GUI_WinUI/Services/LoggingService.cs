using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace MI_GUI_WinUI.Services;

public class LoggingService
{
    private readonly string _logFolder = "Logs";
    private readonly string _logFileName = "window_management.log";
    private readonly object _logLock = new object();

    public async Task LogAsync(string message, Exception? exception = null)
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var logFolder = await localFolder.CreateFolderAsync(_logFolder, CreationCollisionOption.OpenIfExists);
            var logFile = await logFolder.CreateFileAsync(_logFileName, CreationCollisionOption.OpenIfExists);

            string logMessage;
            lock (_logLock)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                logMessage = $"[{timestamp}] {message}";
            
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
        catch
        {
            // Silently fail if logging fails
            // We don't want logging failures to affect the application
        }
    }

    public void Log(string message, Exception? exception = null)
    {
        // Fire and forget async logging
        _ = LogAsync(message, exception);
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
            return $"Error reading logs: {ex.Message}";
        }
    }

    public async Task ClearLogsAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var logFolder = await localFolder.CreateFolderAsync(_logFolder, CreationCollisionOption.OpenIfExists);
            var logFile = await logFolder.CreateFileAsync(_logFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(logFile, string.Empty);
        }
        catch
        {
            // Silently fail if clearing logs fails
        }
    }

    public void LogError(string message, Exception? exception = null)
    {
        Log($"ERROR: {message}", exception);
    }

    public void LogWarning(string message)
    {
        Log($"WARNING: {message}");
    }

    public void LogInfo(string message)
    {
        Log($"INFO: {message}");
    }

    public void LogDebug(string message)
    {
        Log($"DEBUG: {message}");
    }
}