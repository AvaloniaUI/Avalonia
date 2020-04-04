using System;
using System.Collections.Specialized;
using Avalonia.Collections;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="INotifyCollectionChanged"/> subscribers on
    /// <see cref="AvaloniaList{T}"/>
    /// </summary>
    public interface INotifyCollectionChangedDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="INotifyCollectionChanged.CollectionChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[] GetCollectionChangedSubscribers();
    }
}
