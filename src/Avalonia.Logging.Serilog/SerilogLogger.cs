// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Serilog;
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
        private readonly Dictionary<string, ILogger> _areas = new Dictionary<string, ILogger>();

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

        /// <inheritdoc/>
        public void Log(
            AvaloniaLogEventLevel level, 
            string area, 
            object source, 
            string messageTemplate, 
            params object[] propertyValues)
        {
            ILogger areaLogger;

            if (!_areas.TryGetValue(area, out areaLogger))
            {
                areaLogger = _output.ForContext("Area", area);
                _areas.Add(area, areaLogger);
            }

            areaLogger.Write((SerilogLogEventLevel)level, messageTemplate, propertyValues);
        }
    }
}
