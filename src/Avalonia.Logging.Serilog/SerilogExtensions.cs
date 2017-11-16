using System;
using Avalonia.Controls;
using Serilog;
using SerilogLevel = Serilog.Events.LogEventLevel;

namespace Avalonia.Logging.Serilog
{
    /// <summary>
    /// Extension methods for Serilog logging.
    /// </summary>
    public static class SerilogExtensions
    {
        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Debug"/> sink.
        /// </summary>
        /// <typeparam name="T">The application class type.</typeparam>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <returns>The app builder instance.</returns>
        public static T LogToDebug<T>(
            this T builder,
            LogEventLevel level = LogEventLevel.Warning)
                where T : AppBuilderBase<T>, new()
        {
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Is((SerilogLevel)level)
                .WriteTo.Debug(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
            return builder;
        }

        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Trace"/> sink.
        /// </summary>
        /// <typeparam name="T">The application class type.</typeparam>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <returns>The app builder instance.</returns>
        public static T LogToTrace<T>(
            this T builder,
            LogEventLevel level = LogEventLevel.Warning)
                where T : AppBuilderBase<T>, new()
        {
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Is((SerilogLevel)level)
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
            return builder;
        }
    }
}
