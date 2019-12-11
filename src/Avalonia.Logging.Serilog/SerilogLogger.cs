// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Serilog;
using Serilog.Context;
using AvaloniaLogEventLevel = Avalonia.Logging.LogEventLevel;
using SerilogLogEventLevel = Serilog.Events.LogEventLevel;

namespace Avalonia.Logging.Serilog
{
    /// <summary>
    /// Sends log output to serilog.
    /// </summary>
    public class SerilogLogger : ILogSink
    {
        private readonly ILogger _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLogger"/> class.
        /// </summary>
        /// <param name="output">The serilog logger to use.</param>
        public SerilogLogger(ILogger output)
        {
            _output = output;
        }

        /// <summary>
        /// Initializes the Avalonia logging with a new instance of a <see cref="SerilogLogger"/>.
        /// </summary>
        /// <param name="output">The serilog logger to use.</param>
        public static void Initialize(ILogger output)
        {
            Logger.Sink = new SerilogLogger(output);
        }

        public bool IsEnabled(LogEventLevel level)
        {
            return _output.IsEnabled((SerilogLogEventLevel)level);
        }

        public void Log(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate)
        {
            Contract.Requires<ArgumentNullException>(area != null);
            Contract.Requires<ArgumentNullException>(messageTemplate != null);

            using (PushLogContextProperties(area, source))
            {
                _output.Write((SerilogLogEventLevel)level, messageTemplate);
            }
        }

        public void Log<T0>(
            LogEventLevel level, 
            string area, object source,
            string messageTemplate, 
            T0 propertyValue0)
        {
            Contract.Requires<ArgumentNullException>(area != null);
            Contract.Requires<ArgumentNullException>(messageTemplate != null);

            using (PushLogContextProperties(area, source))
            {
                _output.Write((SerilogLogEventLevel)level, messageTemplate, propertyValue0);
            }
        }

        public void Log<T0, T1>(
            LogEventLevel level, 
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1)
        {
            Contract.Requires<ArgumentNullException>(area != null);
            Contract.Requires<ArgumentNullException>(messageTemplate != null);

            using (PushLogContextProperties(area, source))
            {
                _output.Write((SerilogLogEventLevel)level, messageTemplate, propertyValue0, propertyValue1);
            }
        }

        public void Log<T0, T1, T2>(
            LogEventLevel level, 
            string area, 
            object source, 
            string messageTemplate, 
            T0 propertyValue0,
            T1 propertyValue1, 
            T2 propertyValue2)
        {
            Contract.Requires<ArgumentNullException>(area != null);
            Contract.Requires<ArgumentNullException>(messageTemplate != null);

            using (PushLogContextProperties(area, source))
            {
                _output.Write((SerilogLogEventLevel)level, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
            }
        }

        /// <inheritdoc/>
        public void Log(
            AvaloniaLogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Contract.Requires<ArgumentNullException>(area != null);
            Contract.Requires<ArgumentNullException>(messageTemplate != null);

            using (PushLogContextProperties(area, source))
            {
                _output.Write((SerilogLogEventLevel)level, messageTemplate, propertyValues);
            }
        }

        private static LogContextDisposable PushLogContextProperties(string area, object source)
        {
            return new LogContextDisposable(
                LogContext.PushProperty("Area", area),
                LogContext.PushProperty("SourceType", source?.GetType()),
                LogContext.PushProperty("SourceHash", source?.GetHashCode())
                );
        }
        
        private readonly struct LogContextDisposable : IDisposable
        {
            private readonly IDisposable _areaDisposable;
            private readonly IDisposable _sourceTypeDisposable;
            private readonly IDisposable _sourceHashDisposable;

            public LogContextDisposable(IDisposable areaDisposable, IDisposable sourceTypeDisposable, IDisposable sourceHashDisposable)
            {
                _areaDisposable = areaDisposable;
                _sourceTypeDisposable = sourceTypeDisposable;
                _sourceHashDisposable = sourceHashDisposable;
            }

            public void Dispose()
            {
                _areaDisposable.Dispose();
                _sourceTypeDisposable.Dispose();
                _sourceHashDisposable.Dispose();
            }
        }
    }
}
