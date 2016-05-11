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
