using System;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Service interface for application logging
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Logs an information message
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogInformation(string message);

        /// <summary>
        /// Logs an error with optional exception details
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="message">Additional error message</param>
        void LogError(Exception ex, string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The debug message to log</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs a critical error
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="message">Additional error message</param>
        void LogCritical(Exception ex, string message);
    }
}