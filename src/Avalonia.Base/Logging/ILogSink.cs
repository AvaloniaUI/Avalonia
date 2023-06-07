namespace Avalonia.Logging
{
    /// <summary>
    /// Defines a sink for Avalonia logging messages.
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Checks if given log level and area is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The log area.</param>
        /// <returns><see langword="true"/> if given log level is enabled.</returns>
        bool IsEnabled(LogEventLevel level, string area);

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
            object? source,
            string messageTemplate);

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
            object? source,
            string messageTemplate, 
            params object?[] propertyValues);
    }
}
