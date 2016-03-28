// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Perspex.Logging;

namespace Perspex.UnitTests
{
    public delegate void LogCallback(
        LogEventLevel level,
        string area,
        object source,
        string messageTemplate,
        params object[] propertyValues);

    public class TestLogSink : ILogSink
    {
        private LogCallback _callback;

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

        public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            _callback(level, area, source, messageTemplate, propertyValues);
        }
    }
}
