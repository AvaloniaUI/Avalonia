using System;

namespace Avalonia
{
    /// <summary>
    /// Exception signifying an internal logic error in Avalonia.
    /// </summary>
    public class AvaloniaInternalException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaInternalException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public AvaloniaInternalException(string message)
            : base(message)
        {
        }
    }
}
