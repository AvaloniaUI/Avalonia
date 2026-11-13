#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Logging;

namespace Avalonia
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Returns the minimum level to log for a log area.
        /// </summary>
        /// <param name="area">The log area. Well-known values are listed in <see cref="LogArea"/>.</param>
        /// <returns>The minimum level to log for <paramref name="area"/>.</returns>
        public delegate LogEventLevel LogToTraceAreaVerbosity(string area);

        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Trace"/> sink.
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <param name="areas">The areas to log. Valid values are listed in <see cref="LogArea"/>.</param>
        /// <returns>The app builder instance.</returns>
        public static AppBuilder LogToTrace(this AppBuilder builder,
            LogEventLevel level = LogEventLevel.Warning, params string[] areas) =>
            LogToDelegate(builder, s => Trace.WriteLine(s), level, areas);

        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Trace"/> sink.
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="verbosityCallback">The callback that returns the minimum level to log for each area.</param>
        /// <returns>The app builder instance.</returns>
        public static AppBuilder LogToTrace(this AppBuilder builder, LogToTraceAreaVerbosity verbosityCallback) =>
            LogToDelegate(builder, s => Trace.WriteLine(s), verbosityCallback);


        /// <summary>
        /// Logs Avalonia events to a TextWriter
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="writer">The TextWriter that's used for log events.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <param name="areas">The areas to log. Valid values are listed in <see cref="LogArea"/>.</param>
        /// <returns>The app builder instance.</returns>
        public static AppBuilder LogToTextWriter(this AppBuilder builder, TextWriter writer,
            LogEventLevel level = LogEventLevel.Warning, params string[] areas) =>
            LogToDelegate(builder, TextWriter.Synchronized(writer).WriteLine, level, areas);
        
        
        /// <summary>
        /// Logs Avalonia events to a custom delegate
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="logCallback">The callback that's used for log events.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <param name="areas">The areas to log. Valid values are listed in <see cref="LogArea"/>.</param>
        /// <returns>The app builder instance.</returns>
        public static AppBuilder LogToDelegate(
            this AppBuilder builder,
            Action<string> logCallback,
            LogEventLevel level = LogEventLevel.Warning,
            params string[] areas)
        {
            Logger.Sink = new StringLogSink(logCallback, level, areas);
            return builder;
        }

        /// <summary>
        /// Logs Avalonia events to a custom delegate
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="logCallback">The callback that's used for log events.</param>
        /// <param name="verbosityCallback">The callback that returns the minimum level to log for each area.</param>
        /// <returns>The app builder instance.</returns>
        public static AppBuilder LogToDelegate(
            this AppBuilder builder,
            Action<string> logCallback,
            LogToTraceAreaVerbosity verbosityCallback)
        {
            Logger.Sink = new StringLogSink(logCallback, verbosityCallback.Invoke);
            return builder;
        }
    }
}
