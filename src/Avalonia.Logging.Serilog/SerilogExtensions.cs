using System;
using Avalonia.Controls;
using Serilog;
using Serilog.Configuration;
using Serilog.Filters;
using SerilogLevel = Serilog.Events.LogEventLevel;

namespace Avalonia.Logging.Serilog
{
    /// <summary>
    /// Extension methods for Serilog logging.
    /// </summary>
    public static class SerilogExtensions
    {
        private const string DefaultTemplate = "[{Area}] {Message} ({SourceType} #{SourceHash})";

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
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: DefaultTemplate)
                .CreateLogger());
            return builder;
        }

        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Debug"/> sink.
        /// </summary>
        /// <typeparam name="T">The application class type.</typeparam>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="area">The area to log. Valid values are listed in <see cref="LogArea"/>.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <returns>The app builder instance.</returns>
        public static T LogToDebug<T>(
            this T builder,
            string area,
            LogEventLevel level = LogEventLevel.Warning)
                where T : AppBuilderBase<T>, new()
        {
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Is((SerilogLevel)level)
                .Filter.ByIncludingOnly(Matching.WithProperty("Area", area))
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: DefaultTemplate)
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
                .Enrich.FromLogContext()
                .WriteTo.Trace(outputTemplate: DefaultTemplate)
                .CreateLogger());
            return builder;
        }

        /// <summary>
        /// Logs Avalonia events to the <see cref="System.Diagnostics.Trace"/> sink.
        /// </summary>
        /// <typeparam name="T">The application class type.</typeparam>
        /// <param name="builder">The app builder instance.</param>
        /// <param name="area">The area to log. Valid values are listed in <see cref="LogArea"/>.</param>
        /// <param name="level">The minimum level to log.</param>
        /// <returns>The app builder instance.</returns>
        public static T LogToTrace<T>(
            this T builder,
            string area,
            LogEventLevel level = LogEventLevel.Warning)
                where T : AppBuilderBase<T>, new()
        {
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Is((SerilogLevel)level)
                .Filter.ByIncludingOnly(Matching.WithProperty("Area", area))
                .Enrich.FromLogContext()
                .WriteTo.Trace(outputTemplate: DefaultTemplate)
                .CreateLogger());
            return builder;
        }
    }
}
