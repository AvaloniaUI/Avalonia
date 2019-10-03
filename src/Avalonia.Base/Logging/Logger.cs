// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Avalonia.Logging
{
    /// <summary>
    /// Logs avalonia messages.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets or sets the application-defined sink that receives the messages.
        /// </summary>
        public static ILogSink Sink { get; set; }

        /// <summary>
        /// Checks if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <returns><see langword="true"/> if given log level is enabled.</returns>
        public static bool IsEnabled(LogEventLevel level)
        {
            return Sink?.IsEnabled(level) == true;
        }

        /// <summary>
        /// Returns parametrized logging sink if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <returns>Log sink or <see langword="null"/> if log level is not enabled.</returns>
        public static ParametrizedLogger? TryGetLogger(LogEventLevel level)
        {
            if (!IsEnabled(level))
            {
                return null;
            }

            return new ParametrizedLogger(Sink, level);
        }

        /// <summary>
        /// Returns parametrized logging sink if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="outLogger">Log sink that is valid only if method returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> if logger was obtained successfully.</returns>
        public static bool TryGetLogger(LogEventLevel level, out ParametrizedLogger outLogger)
        {
            ParametrizedLogger? logger = TryGetLogger(level);

            outLogger = logger.GetValueOrDefault();

            return logger.HasValue;
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate)
        {
            Sink?.Log(level, area, source, messageTemplate);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log<T0>(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0)
        {
            Sink?.Log(level, area, source, messageTemplate, propertyValue0);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log<T0, T1>(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1)
        {
            Sink?.Log(level, area, source, messageTemplate, propertyValue0, propertyValue1);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log<T0, T1, T2>(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2)
        {
            Sink?.Log(level, area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(
            LogEventLevel level, 
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues)
        {
            Sink?.Log(level, area, source, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Logs an event with the <see cref="LogEventLevel.Verbose"/> level.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Verbose(
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues)
        {
            Log(LogEventLevel.Verbose, area, source, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Logs an event with the <see cref="LogEventLevel.Debug"/> level.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Debug, area, source, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Logs an event with the <see cref="LogEventLevel.Information"/> level.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Information(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Information, area, source, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Logs an event with the <see cref="LogEventLevel.Warning"/> level.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Warning, area, source, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Logs an event with the <see cref="LogEventLevel.Error"/> level.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues)
        {
            Log(LogEventLevel.Error, area, source, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Logs an event with the <see cref="LogEventLevel.Fatal"/> level.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fatal(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Fatal, area, source, messageTemplate, propertyValues);
        }
    }
}
