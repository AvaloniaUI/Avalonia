// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        public static ParametrizedLogger? TryGet(LogEventLevel level)
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
        public static bool TryGet(LogEventLevel level, out ParametrizedLogger outLogger)
        {
            ParametrizedLogger? logger = TryGet(level);

            outLogger = logger.GetValueOrDefault();

            return logger.HasValue;
        }
    }
}
