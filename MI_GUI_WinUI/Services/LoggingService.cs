using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace MI_GUI_WinUI.Services;

public class LoggingService
{
    private static readonly string LogFolder = "Logs";
    private static readonly string LogFileName = "window_management.log";
    private static readonly object LogLock = new object();

    public static async Task LogAsync(string message, Exception? exception = null)
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var logFolder = await localFolder.CreateFolderAsync(LogFolder, CreationCollisionOption.OpenIfExists);
            var logFile = await logFolder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            
            if (exception != null)
            {
                logMessage += $"\nException: {exception.GetType().Name}\nMessage: {exception.Message}\nStack Trace:\n{exception.StackTrace}";
                if (exception.InnerException != null)
                {
                    logMessage += $"\nInner Exception: {exception.InnerException.Message}";
                }
            }

            logMessage += "\n----------------------------------------\n";

            await FileIO.AppendTextAsync(logFile, logMessage);
        }
        catch
        {
            // Silently fail if logging fails
            // We don't want logging failures to affect the application
        }
    }

    public static void Log(string message, Exception? exception = null)
    {
        // Fire and forget async logging
        _ = LogAsync(message, exception);
    }

    public static async Task<string> GetLogsAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var logFolder = await localFolder.CreateFolderAsync(LogFolder, CreationCollisionOption.OpenIfExists);
            var logFile = await logFolder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);

            return await FileIO.ReadTextAsync(logFile);
        }
        catch (Exception ex)
        {
            return $"Error reading logs: {ex.Message}";
        }
    }

    public static async Task ClearLogsAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var logFolder = await localFolder.CreateFolderAsync(LogFolder, CreationCollisionOption.OpenIfExists);
            var logFile = await logFolder.CreateFileAsync(LogFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(logFile, string.Empty);
        }
        catch
        {
            // Silently fail if clearing logs fails
        }
    }
}