using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace Avalonia.Utilities;

/// <summary>
/// Manages subscriptions to events using weak listeners.
/// </summary>
public class WeakEvent<TSender, TEventArgs> : WeakEvent where TEventArgs : EventArgs where TSender : class
{
    private readonly Func<TSender, EventHandler<TEventArgs>, Action> _subscribe;

    readonly ConditionalWeakTable<object, Subscription> _subscriptions = new();

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

    private class Subscription
    {
        private readonly WeakEvent<TSender, TEventArgs> _ev;
        private readonly TSender _target;
        private readonly Action _compact;

        struct Entry
        {
            WeakReference<IWeakEventSubscriber<TEventArgs>>? _reference;
            int _hashCode;

            public Entry(IWeakEventSubscriber<TEventArgs> r)
            {
                if (r == null)
                {
                    _reference = null;
                    _hashCode = 0;
                    return;
                }

                _hashCode = r.GetHashCode();
                _reference = new WeakReference<IWeakEventSubscriber<TEventArgs>>(r);
            }

            public bool IsEmpty
            {
                get
                {
                    if (_reference == null)
                        return true;
                    if (_reference.TryGetTarget(out _))
                        return false;
                    _reference = null;
                    return true;
                }
            }

            public bool TryGetTarget([MaybeNullWhen(false)]out IWeakEventSubscriber<TEventArgs> target)
            {
                if (_reference == null)
                {
                    target = null!;
                    return false;
                }
                return _reference.TryGetTarget(out target);
            }

            public bool Equals(IWeakEventSubscriber<TEventArgs> r)
            {
                if (_reference == null || r.GetHashCode() != _hashCode)
                    return false;
                return _reference.TryGetTarget(out var target) && target == r;
            }
        }

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

        void Destroy()
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

        void ScheduleCompact()
        {
            if(_compactScheduled || _destroyed)
                return;
            _compactScheduled = true;
            Dispatcher.UIThread.Post(_compact, DispatcherPriority.Background);
        }
        
        void Compact()
        {
            if(!_compactScheduled)
                return;
            _compactScheduled = false;
            _list.Compact();
            if (_list.IsEmpty)
                Destroy();
        }

        void OnEvent(object? sender, TEventArgs eventArgs)
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