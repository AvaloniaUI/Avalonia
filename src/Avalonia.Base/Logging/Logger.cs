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
        public static ILogSink? Sink { get; set; }

        /// <summary>
        /// Checks if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The log area.</param>
        /// <returns><see langword="true"/> if given log level is enabled.</returns>
        public static bool IsEnabled(LogEventLevel level, string area)
        {
            return Sink?.IsEnabled(level, area) == true;
        }

        /// <summary>
        /// Returns parametrized logging sink if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates from.</param>
        /// <returns>Log sink or <see langword="null"/> if log level is not enabled.</returns>
        public static ParametrizedLogger? TryGet(LogEventLevel level, string area)
        {
            if (!IsEnabled(level, area))
            {
                return null;
            }

            return new ParametrizedLogger(Sink!, level, area);
        }

        /// <summary>
        /// Returns parametrized logging sink if given log level is enabled.
        /// </summary>
        /// <param name="level">The log event level.</param>
        /// <param name="area">The area that the event originates from.</param>
        /// <param name="outLogger">Log sink that is valid only if method returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> if logger was obtained successfully.</returns>
        public static bool TryGet(LogEventLevel level, string area, out ParametrizedLogger outLogger)
        {
            ParametrizedLogger? logger = TryGet(level, area);

            outLogger = logger.GetValueOrDefault();

            return logger.HasValue;
        }
    }
}
