using System;
using System.Reactive.Disposables;
using Avalonia.Logging;

namespace Avalonia.UnitTests
{
    public delegate void LogCallback(
        LogEventLevel level,
        string area,
        object source,
        string messageTemplate,
        params object[] propertyValues);

    public class TestLogSink : ILogSink
    {
        private readonly LogCallback _callback;

        public TestLogSink(LogCallback callback)
        {
            _callback = callback;
        }

        public static IDisposable Start(LogCallback callback)
        {
            var sink = new TestLogSink(callback);
            Logger.Sink = sink;
            return Disposable.Create(() => Logger.Sink = null);
        }

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return true;
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate)
        {
            _callback(level, area, source, messageTemplate);
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate,
            params object[] propertyValues)
        {
            _callback(level, area, source, messageTemplate, propertyValues);
        }
    }
}
