using System.Runtime.CompilerServices;

namespace Avalonia.Logging
{
    /// <summary>
    /// Logger sink parametrized for given logging level.
    /// </summary>
    public readonly record struct ParametrizedLogger
    {
        private readonly ILogSink _sink;
        private readonly LogEventLevel _level;
        private readonly string _area;

        public ParametrizedLogger(ILogSink sink, LogEventLevel level, string area)
        {
            _sink = sink;
            _level = level;
            _area = area;
        }

        /// <summary>
        /// Checks if this logger can be used.
        /// </summary>
        public bool IsValid => _sink != null;

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(
            object? source,
            string messageTemplate)
        {
            _sink.Log(_level, _area, source, messageTemplate);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0>(
            object? source,
            string messageTemplate,
            T0 propertyValue0)
        {
            _sink.Log(_level, _area, source, messageTemplate, propertyValue0);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1>(
            object? source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1)
        {
            _sink.Log(_level, _area, source, messageTemplate, propertyValue0, propertyValue1);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2>(
            object? source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2)
        {
            _sink.Log(_level, _area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        /// <param name="propertyValue3">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2, T3>(
            object? source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2,
            T3 propertyValue3)
        {
            _sink.Log(_level, _area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2, propertyValue3);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
        /// <param name="source">The object from which the event originates.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="propertyValue0">Message property value.</param>
        /// <param name="propertyValue1">Message property value.</param>
        /// <param name="propertyValue2">Message property value.</param>
        /// <param name="propertyValue3">Message property value.</param>
        /// <param name="propertyValue4">Message property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log<T0, T1, T2, T3, T4>(
            object? source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2,
            T3 propertyValue3,
            T4 propertyValue4)
        {
            _sink.Log(_level, _area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2, propertyValue3, propertyValue4);
        }

        /// <summary>
        /// Logs an event.
        /// </summary>
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
            object? source,
            string messageTemplate,
            T0 propertyValue0,
            T1 propertyValue1,
            T2 propertyValue2,
            T3 propertyValue3,
            T4 propertyValue4,
            T5 propertyValue5)
        {
            _sink.Log(_level, _area, source, messageTemplate, propertyValue0, propertyValue1, propertyValue2, propertyValue3, propertyValue4, propertyValue5);
        }
    }
}
