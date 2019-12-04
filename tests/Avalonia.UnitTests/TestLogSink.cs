// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        public bool IsEnabled(LogEventLevel level)
        {
            return true;
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate)
        {
            _callback(level, area, source, messageTemplate);
        }

        public void Log<T0>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0)
        {
            _callback(level, area, source, messageTemplate, propertyValue0);
        }

        public void Log<T0, T1>(LogEventLevel level, string area, object source, string messageTemplate,
            T0 propertyValue0, T1 propertyValue1)
        {
            _callback(level, area, source, messageTemplate, propertyValue0, propertyValue1);
        }

        public void Log<T0, T1, T2>(LogEventLevel level, string area, object source, string messageTemplate,
            T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
        {
            _callback(level, area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate,
            params object[] propertyValues)
        {
            _callback(level, area, source, messageTemplate, propertyValues);
        }
    }
}
