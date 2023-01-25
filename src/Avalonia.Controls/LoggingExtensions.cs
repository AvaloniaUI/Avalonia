using Avalonia.Controls;
using Avalonia.Logging;

namespace Avalonia
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Trace"/> sink.
        /// </summary>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <param name="areas">The areas to log. Valid values are listed in <see cref="LogArea"/>.</param>
        /// <returns>The app builder instance.</returns>
        public static AppBuilder LogToTrace(
            this AppBuilder builder,
            LogEventLevel level = LogEventLevel.Warning,
            params string[] areas)
        {
            Logger.Sink = new TraceLogSink(level, areas);
            return builder;
        }
    }
}
