using System;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace Avalonia.Utilities;

/// <summary>
/// Manages subscriptions to events using weak listeners.
/// </summary>
public sealed class WeakEvent<TSender, TEventArgs> : WeakEvent where TEventArgs : EventArgs where TSender : class
{
    private readonly Func<TSender, EventHandler<TEventArgs>, Action> _subscribe;

    private readonly ConditionalWeakTable<object, Subscription> _subscriptions = new();

    internal WeakEvent(
        Action<TSender, EventHandler<TEventArgs>> subscribe,
        Action<TSender, EventHandler<TEventArgs>> unsubscribe)
    {
        _subscribe = (t, s) =>
        {
            subscribe(t, s);
            return () => unsubscribe(t, s);
        };
    }
    
    internal WeakEvent(Func<TSender, EventHandler<TEventArgs>, Action> subscribe)
    {
        _subscribe = subscribe;
    }
    
    public void Subscribe(TSender target, IWeakEventSubscriber<TEventArgs> subscriber)
    {
        if (!_subscriptions.TryGetValue(target, out var subscription))
            _subscriptions.Add(target, subscription = new Subscription(this, target));
        subscription.Add(subscriber);
    }

    public void Unsubscribe(TSender target, IWeakEventSubscriber<TEventArgs> subscriber)
    {
        if (_subscriptions.TryGetValue(target, out var subscription)) 
            subscription.Remove(subscriber);
    }

    private sealed class Subscription
    {
        private readonly WeakEvent<TSender, TEventArgs> _ev;
        private readonly TSender _target;
        private readonly Action _compact;
        private readonly Action _unsubscribe;
        private readonly WeakHashList<IWeakEventSubscriber<TEventArgs>> _list = new();
        private bool _compactScheduled;
        private bool _destroyed;

        public Subscription(WeakEvent<TSender, TEventArgs> ev, TSender target)
        {
            _ev = ev;
            _target = target;
            _compact = Compact;
            _unsubscribe = ev._subscribe(target, OnEvent);
        }

        private void Destroy()
        {
            if(_destroyed)
                return;
            _destroyed = true;
            _unsubscribe();
            _ev._subscriptions.Remove(_target);
        }

        public void Add(IWeakEventSubscriber<TEventArgs> s) => _list.Add(s);

        public void Remove(IWeakEventSubscriber<TEventArgs> s)
        {
            _list.Remove(s);
            if(_list.IsEmpty)
                Destroy();
            else if(_list.NeedCompact && _compactScheduled)
                ScheduleCompact();
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
            if(!_compactScheduled)
                return;
            _compactScheduled = false;
            _list.Compact();
            if (_list.IsEmpty)
                Destroy();
        }

        private void OnEvent(object? sender, TEventArgs eventArgs)
        {
            var alive = _list.GetAlive();
            if(alive == null)
                Destroy();
            else
            {
                foreach(var item in alive.Span)
                    item.OnEvent(_target, _ev, eventArgs);
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
        Action<TSender, EventHandler<TEventArgs>> unsubscribe) where TSender : class where TEventArgs : EventArgs
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
