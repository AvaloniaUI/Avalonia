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
        /// Gets or sets the application-defined sink that recieves the messages.
        /// </summary>
        public static ILogSink Sink { get; set; }

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
