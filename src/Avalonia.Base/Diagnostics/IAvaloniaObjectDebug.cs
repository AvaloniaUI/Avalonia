using System;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="AvaloniaObject"/>.
    /// </summary>
    internal interface IAvaloniaObjectDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="AvaloniaObject.PropertyChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[]? GetPropertyChangedSubscribers();
    }
}
