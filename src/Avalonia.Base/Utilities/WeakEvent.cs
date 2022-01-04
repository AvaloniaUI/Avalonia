using System;
using System.Collections.Generic;
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
        subscription.Add(new WeakReference<IWeakEventSubscriber<TEventArgs>>(subscriber));
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

        private WeakReference<IWeakEventSubscriber<TEventArgs>>?[] _data =
            new WeakReference<IWeakEventSubscriber<TEventArgs>>[16];
        private int _count;
        private readonly Action _unsubscribe;
        private bool _compactScheduled;

        public Subscription(WeakEvent<TSender, TEventArgs> ev, TSender target)
        {
            _ev = ev;
            _target = target;
            _compact = Compact;
            _unsubscribe = ev._subscribe(target, OnEvent);
        }

        void Destroy()
        {
            _unsubscribe();
            _ev._subscriptions.Remove(_target);
        }

        public void Add(WeakReference<IWeakEventSubscriber<TEventArgs>> s)
        {
            if (_count == _data.Length)
            {
                //Extend capacity
                var extendedData = new WeakReference<IWeakEventSubscriber<TEventArgs>>?[_data.Length * 2];
                Array.Copy(_data, extendedData, _data.Length);
                _data = extendedData;
            }

            _data[_count] = s;
            _count++;
        }

        public void Remove(IWeakEventSubscriber<TEventArgs> s)
        {
            var removed = false;

            for (int c = 0; c < _count; ++c)
            {
                var reference = _data[c];

                if (reference != null && reference.TryGetTarget(out var instance) && instance == s)
                {
                    _data[c] = null;
                    removed = true;
                }
            }

            if (removed)
            {
                ScheduleCompact();
            }
        }

        void ScheduleCompact()
        {
            if(_compactScheduled)
                return;
            _compactScheduled = true;
            Dispatcher.UIThread.Post(_compact, DispatcherPriority.Background);
        }
        
        void Compact()
        {
            _compactScheduled = false;
            int empty = -1;
            for (var c = 0; c < _count; c++)
            {
                var r = _data[c];
                //Mark current index as first empty
                if (r == null && empty == -1)
                    empty = c;
                //If current element isn't null and we have an empty one
                if (r != null && empty != -1)
                {
                    _data[c] = null;
                    _data[empty] = r;
                    empty++;
                }
            }

            if (empty != -1)
                _count = empty;
            if (_count == 0)
                Destroy();
        }

        void OnEvent(object? sender, TEventArgs eventArgs)
        {
            var needCompact = false;
            for (var c = 0; c < _count; c++)
            {
                var r = _data[c];
                if (r?.TryGetTarget(out var sub) == true)
                    sub!.OnEvent(_target, _ev, eventArgs);
                else
                    needCompact = true;
            }

            if (needCompact)
                ScheduleCompact();
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