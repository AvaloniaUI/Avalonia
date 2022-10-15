using System;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Defines a listener to a event subscribed vis the <see cref="WeakObservable"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event arguments.</typeparam>
    public interface IWeakSubscriber<T> where T : EventArgs
    {
        /// <summary>
        /// Invoked when the subscribed event is raised.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnEvent(object? sender, T e);
    }
}
