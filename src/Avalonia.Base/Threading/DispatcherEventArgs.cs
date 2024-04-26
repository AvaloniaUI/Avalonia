using System;

namespace Avalonia.Threading;

/// <summary>
/// Provides event data for Dispatcher related events.
/// </summary>
public abstract class DispatcherEventArgs : EventArgs
{
    /// <summary>
    /// The Dispatcher associated with this event.
    /// </summary>
    public Dispatcher Dispatcher { get; }

    internal DispatcherEventArgs(Dispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }
}
