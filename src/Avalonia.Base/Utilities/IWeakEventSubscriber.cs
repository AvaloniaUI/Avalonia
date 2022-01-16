using System;

namespace Avalonia.Utilities;

/// <summary>
/// Defines a listener to a event subscribed vis the <see cref="WeakEvent{TTarget, TEventArgs}"/>.
/// </summary>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
public interface IWeakEventSubscriber<in TEventArgs> where TEventArgs : EventArgs
{
    void OnEvent(object? sender, WeakEvent ev, TEventArgs e);
}

public class WeakEventSubscriber<TEventArgs> : IWeakEventSubscriber<TEventArgs> where TEventArgs : EventArgs 
{
    public event Action<object?, WeakEvent, TEventArgs>? Event;

    void IWeakEventSubscriber<TEventArgs>.OnEvent(object? sender, WeakEvent ev, TEventArgs e)
    {
        
    }
}