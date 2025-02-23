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
            _loggingService.Log(logMessage, exception);
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
            return new CustomLogger<object>(_loggingService);
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}