using Microsoft.Extensions.Logging;
using System;

namespace MI_GUI_WinUI.Services
{
    public class CustomLogger<T> : ILogger<T>
    {
        private readonly LoggingService _loggingService;
        private readonly string _categoryName;

        public CustomLogger(LoggingService loggingService)
        {
            _loggingService = loggingService;
            _categoryName = typeof(T).Name;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var logMessage = $"[{_categoryName}] [{logLevel}] {message}";

            switch (logLevel)
            {
                case LogLevel.Critical:
                    _loggingService.LogCritical(exception, logMessage);
                    break;
                case LogLevel.Error:
                    _loggingService.LogError(exception, logMessage);
                    break;
                case LogLevel.Warning:
                    _loggingService.LogWarning(logMessage);
                    break;
                case LogLevel.Information:
                    _loggingService.LogInformation(logMessage);
                    break;
                case LogLevel.Debug:
                    _loggingService.LogDebug(logMessage);
                    break;
                default:
                    _loggingService.LogInformation(logMessage);
                    break;
            }
        }
    }

    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly LoggingService _loggingService;

        public CustomLoggerProvider(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var loggerType = typeof(CustomLogger<>).MakeGenericType(Type.GetType(categoryName) ?? typeof(object));
            return (ILogger)Activator.CreateInstance(loggerType, _loggingService);
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}