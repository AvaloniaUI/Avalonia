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

public sealed class WeakEventSubscriber<TEventArgs> : IWeakEventSubscriber<TEventArgs> where TEventArgs : EventArgs 
{
    public event Action<object?, WeakEvent, TEventArgs>? Event;

    void IWeakEventSubscriber<TEventArgs>.OnEvent(object? sender, WeakEvent ev, TEventArgs e)
    {
        Event?.Invoke(sender, ev, e);
    }
}

public sealed class TargetWeakEventSubscriber<TTarget, TEventArgs> : IWeakEventSubscriber<TEventArgs> where TEventArgs : EventArgs
{
    private readonly TTarget _target;
    private readonly Action<TTarget, object?, WeakEvent, TEventArgs> _dispatchFunc;

    public TargetWeakEventSubscriber(TTarget target, Action<TTarget, object?, WeakEvent, TEventArgs> dispatchFunc)
    {
        _target = target;
        _dispatchFunc = dispatchFunc;
    }

    void IWeakEventSubscriber<TEventArgs>.OnEvent(object? sender, WeakEvent ev, TEventArgs e)
    {
        _dispatchFunc(_target, sender, ev, e);
    }
}
