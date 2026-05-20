using Microsoft.Extensions.Logging;

namespace SystemMonitorAgent.Host.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;

        var directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _filePath, _lock);
    }

    public void Dispose()
    {
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _filePath;
        private readonly object _lock;

        public FileLogger(string categoryName, string filePath, object @lock)
        {
            _categoryName = categoryName;
            _filePath = filePath;
            _lock = @lock;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"{DateTimeOffset.Now:O} [{logLevel}] {_categoryName}: {formatter(state, exception)}";
            if (exception is not null)
            {
                message = $"{message}{Environment.NewLine}{exception}";
            }

            lock (_lock)
            {
                File.AppendAllText(_filePath, $"{message}{Environment.NewLine}");
            }
        }
    }
}
