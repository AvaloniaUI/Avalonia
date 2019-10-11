// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Logging
{
    /// <summary>
    /// Defines a sink for Avalonia logging messages.
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Checks if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <returns><see langword="true"/> if given log level is enabled.</returns>
        bool IsEnabled(LogEventLevel level);

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        void Log(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate);

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        void Log<T0>(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0);

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        void Log<T0, T1>(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1);

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
        void Log<T0, T1, T2>(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2);

        /// <summary>
        /// Logs a new event.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValues">The message property values.</param>
        void Log(
            LogEventLevel level,
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues);
    }
}
