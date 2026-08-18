using System;
using Avalonia.Logging;
using Avalonia.Reactive;
using Microsoft.Extensions.Logging;

namespace Avalonia.Headless.Vnc;

internal class AvaloniaVncLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Logger.TryGet(ToLogEventLevel(logLevel), LogArea.VncPlatform)
            ?.Log(state, formatter(state,exception));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return Logger.IsEnabled(ToLogEventLevel(logLevel), LogArea.VncPlatform);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return Disposable.Empty;
    }

    private static LogEventLevel ToLogEventLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}
