// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Avalonia.Logging
{
    /// <summary>
    /// Logger sink parametrized for given logging level.
    /// </summary>
    public readonly struct ParametrizedLogger
    {
        private readonly ILogSink _sink;
        private readonly LogEventLevel _level;

        public ParametrizedLogger(ILogSink sink, LogEventLevel level)
        {
            _sink = sink;
            _level = level;
        }

        /// <summary>
        /// Checks if this logger can be used.
        /// </summary>
        public bool IsValid => _sink != null;

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(
            string area,
            object source,
            string messageTemplate)
        {
            _sink.Log(_level, area, source, messageTemplate);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0>(
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0)
        {
            _sink.Log(_level, area, source, messageTemplate, propertyValue0);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1>(
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1)
        {
            _sink.Log(_level, area, source, messageTemplate, propertyValue0, propertyValue1);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2>(
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2)
        {
            _sink.Log(_level, area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        /// <param name="propertyValue3">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2, T3>(
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2,
            T3 propertyValue3)
        {
            _sink.Log(_level, area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2, propertyValue3);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        /// <param name="propertyValue3">Message property value.</param>
        /// <param name="propertyValue4">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2, T3, T4>(
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2,
            T3 propertyValue3,
            T4 propertyValue4)
        {
            _sink.Log(_level, area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2, propertyValue3, propertyValue4);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="area">The area that the event originates.</param>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        /// <param name="propertyValue3">Message property value.</param>
        /// <param name="propertyValue4">Message property value.</param>
        /// <param name="propertyValue5">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2, T3, T4, T5>(
            string area,
            object source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2,
            T3 propertyValue3,
            T4 propertyValue4,
            T5 propertyValue5)
        {
            _sink.Log(_level, area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2, propertyValue3, propertyValue4, propertyValue5);
        }
    }
}
