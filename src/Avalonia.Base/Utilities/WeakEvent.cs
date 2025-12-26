using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Collections.Pooled;
using Avalonia.Threading;

namespace Avalonia.Utilities;

/// <summary>
/// Manages subscriptions to events using weak listeners.
/// </summary>
public sealed class WeakEvent<TSender, TEventArgs> : WeakEvent where TSender : class
{
    private readonly Func<TSender, EventHandler<TEventArgs>, Action> _subscribe;
    private readonly ConditionalWeakTable<TSender, Subscription> _subscriptions = new();
    private readonly ConditionalWeakTable<TSender, Subscription>.CreateValueCallback _createSubscription;

    internal WeakEvent(
        Action<TSender, EventHandler<TEventArgs>> subscribe,
        Action<TSender, EventHandler<TEventArgs>> unsubscribe)
    {
        _subscribe = (t, s) =>
        {
            subscribe(t, s);
            return () => unsubscribe(t, s);
        };
        _createSubscription = CreateSubscription;
    }

    internal WeakEvent(Func<TSender, EventHandler<TEventArgs>, Action> subscribe)
    {
        _subscribe = subscribe;
        _createSubscription = CreateSubscription;
    }

    public void Subscribe(TSender target, IWeakEventSubscriber<TEventArgs> subscriber)
    {
        var spinWait = default(SpinWait);
        while (true)
        {
            var subscription = _subscriptions.GetValue(target, _createSubscription);
            if (subscription.Add(subscriber))
                break;
            spinWait.SpinOnce();
        }
    }

    public void Unsubscribe(TSender target, IWeakEventSubscriber<TEventArgs> subscriber)
    {
        if (_subscriptions.TryGetValue(target, out var subscription))
            subscription.Remove(subscriber);
    }

    private Subscription CreateSubscription(TSender key) => new(this, key);

    private sealed class Subscription
    {
        private readonly WeakEvent<TSender, TEventArgs> _ev;
        private readonly TSender _target;
        private readonly Action _compact;
        private readonly WeakHashList<IWeakEventSubscriber<TEventArgs>> _list = new();
        private readonly object _lock = new();
        private Action? _unsubscribe;
        private bool _compactScheduled;
        private bool _destroyed;

        public Subscription(WeakEvent<TSender, TEventArgs> ev, TSender target)
        {
            _ev = ev;
            _target = target;
            _compact = Compact;
        }

        private void Destroy()
        {
            if(_destroyed)
                return;
            _destroyed = true;
            _unsubscribe?.Invoke();
            _ev._subscriptions.Remove(_target);
        }

        public bool Add(IWeakEventSubscriber<TEventArgs> s)
        {
            if (_destroyed)
                return false;

            lock (_lock)
            {
                if (_destroyed)
                    return false;

                _unsubscribe ??= _ev._subscribe(_target, OnEvent);
                _list.Add(s);
                return true;
            }
        }

        public void Remove(IWeakEventSubscriber<TEventArgs> s)
        {
            if (_destroyed)
                return;

            lock (_lock)
            {
                if (_destroyed)
                    return;

                _list.Remove(s);
                if(_list.IsEmpty)
                    Destroy();
                else if(_list.NeedCompact && _compactScheduled)
                    ScheduleCompact();
            }
        }

        private void ScheduleCompact()
        {
            if(_compactScheduled || _destroyed)
                return;
            _compactScheduled = true;
            Dispatcher.UIThread.Post(_compact, DispatcherPriority.Background);
        }

        private void Compact()
        {
            if (_destroyed)
                return;

            lock (_lock)
            {
                if (_destroyed)
                    return;
                if(!_compactScheduled)
                    return;
                _compactScheduled = false;
                _list.Compact();
                if (_list.IsEmpty)
                    Destroy();
            }
        }

        private void OnEvent(object? sender, TEventArgs eventArgs)
        {
            PooledList<IWeakEventSubscriber<TEventArgs>>? alive;
            lock (_lock)
            {
                alive = _list.GetAlive();
                if (alive == null)
                {
                    Destroy();
                    return;
                }
            }

            foreach(var item in alive.Span)
                item.OnEvent(_target, _ev, eventArgs);

            lock (_lock)
            {
                WeakHashList<IWeakEventSubscriber<TEventArgs>>.ReturnToSharedPool(alive);
                if(_list.NeedCompact && !_compactScheduled)
                    ScheduleCompact();
            }
        }
    }

}

public class WeakEvent
{
    public static WeakEvent<TSender, TEventArgs> Register<TSender, TEventArgs>(
        Action<TSender, EventHandler<TEventArgs>> subscribe,
        Action<TSender, EventHandler<TEventArgs>> unsubscribe) where TSender : class
    {
        return new WeakEvent<TSender, TEventArgs>(subscribe, unsubscribe);
    }

    public static WeakEvent<TSender, TEventArgs> Register<TSender, TEventArgs>(
        Func<TSender, EventHandler<TEventArgs>, Action> subscribe) where TSender : class where TEventArgs : EventArgs
    {
        return new WeakEvent<TSender, TEventArgs>(subscribe);
    }

    public static WeakEvent<TSender, EventArgs> Register<TSender>(
        Action<TSender, EventHandler> subscribe,
        Action<TSender, EventHandler> unsubscribe) where TSender : class
    {
        return Register<TSender, EventArgs>((s, h) =>
        {
            EventHandler handler = (_, e) => h(s, e);
            subscribe(s, handler);
            return () => unsubscribe(s, handler);
        });
    }
}
