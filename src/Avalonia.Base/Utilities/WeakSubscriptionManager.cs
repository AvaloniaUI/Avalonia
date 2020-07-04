using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Manages subscriptions to events using weak listeners.
    /// </summary>
    public static class WeakSubscriptionManager
    {
        /// <summary>
        /// Subscribes to an event on an object using a weak subscription.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
        /// <param name="target">The event source.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="subscriber">The subscriber.</param>
        public static void Subscribe<TTarget, TEventArgs>(TTarget target, string eventName, IWeakSubscriber<TEventArgs> subscriber)
            where TEventArgs : EventArgs
        {
            var dic = SubscriptionTypeStorage<TEventArgs>.Subscribers.GetOrCreateValue(target);
            Subscription<TEventArgs> sub;

            if (!dic.TryGetValue(eventName, out sub))
            {
                dic[eventName] = sub = new Subscription<TEventArgs>(dic, typeof(TTarget), target, eventName);
            }

            sub.Add(new WeakReference<IWeakSubscriber<TEventArgs>>(subscriber));
        }

        /// <summary>
        /// Unsubscribes from an event.
        /// </summary>
        /// <typeparam name="T">The type of the event arguments.</typeparam>
        /// <param name="target">The event source.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="subscriber">The subscriber.</param>
        public static void Unsubscribe<T>(object target, string eventName, IWeakSubscriber<T> subscriber)
            where T : EventArgs
        {
            SubscriptionDic<T> dic;

            if (SubscriptionTypeStorage<T>.Subscribers.TryGetValue(target, out dic))
            {
                Subscription<T> sub;

                if (dic.TryGetValue(eventName, out sub))
                {
                    sub.Remove(subscriber);
                }
            }
        }

        private static class SubscriptionTypeStorage<T>
            where T : EventArgs
        {
            public static readonly ConditionalWeakTable<object, SubscriptionDic<T>> Subscribers
                = new ConditionalWeakTable<object, SubscriptionDic<T>>();
        }

        private class SubscriptionDic<T> : Dictionary<string, Subscription<T>>
            where T : EventArgs
        {
        }

        private static readonly Dictionary<Type, Dictionary<string, EventInfo>> Accessors
            = new Dictionary<Type, Dictionary<string, EventInfo>>();

        private class Subscription<T> where T : EventArgs
        {
            private readonly EventInfo _info;
            private readonly SubscriptionDic<T> _sdic;
            private readonly object _target;
            private readonly string _eventName;
            private readonly Delegate _delegate;

            private WeakReference<IWeakSubscriber<T>>[] _data = new WeakReference<IWeakSubscriber<T>>[16];
            private int _count = 0;

            public Subscription(SubscriptionDic<T> sdic, Type targetType, object target, string eventName)
            {
                _sdic = sdic;
                _target = target;
                _eventName = eventName;
                Dictionary<string, EventInfo> evDic;
                if (!Accessors.TryGetValue(targetType, out evDic))
                    Accessors[targetType] = evDic = new Dictionary<string, EventInfo>();

                if (!evDic.TryGetValue(eventName, out _info))
                {
                    var ev = targetType.GetRuntimeEvents().FirstOrDefault(x => x.Name == eventName);

                    if (ev == null)
                    {
                        throw new ArgumentException(
                            $"The event {eventName} was not found on {target.GetType()}.");
                    }

                    evDic[eventName] = _info = ev;
                }

                var del = new Action<object, T>(OnEvent);
                _delegate = del.GetMethodInfo().CreateDelegate(_info.EventHandlerType, del.Target);
                _info.AddMethod.Invoke(target, new[] { _delegate });
            }

            void Destroy()
            {
                _info.RemoveMethod.Invoke(_target, new[] { _delegate });
                _sdic.Remove(_eventName);
            }

            public void Add(WeakReference<IWeakSubscriber<T>> s)
            {
                if (_count == _data.Length)
                {
                    //Extend capacity
                    var ndata = new WeakReference<IWeakSubscriber<T>>[_data.Length*2];
                    Array.Copy(_data, ndata, _data.Length);
                    _data = ndata;
                }
                _data[_count] = s;
                _count++;
            }

            public void Remove(IWeakSubscriber<T> s)
            {
                var removed = false;

                for (int c = 0; c < _count; ++c)
                {
                    var reference = _data[c];
                    IWeakSubscriber<T> instance;

                    if (reference != null && reference.TryGetTarget(out instance) && instance == s)
                    {
                        _data[c] = null;
                        removed = true;
                    }
                }

                if (removed)
                {
                    Compact();
                }
            }

            void Compact()
            {
                int empty = -1;
                for (int c = 0; c < _count; c++)
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

            void OnEvent(object sender, T eventArgs)
            {
                var needCompact = false;
                for(var c=0; c<_count; c++)
                {
                    var r = _data[c];
                    IWeakSubscriber<T> sub;
                    if (r.TryGetTarget(out sub))
                        sub.OnEvent(sender, eventArgs);
                    else
                        needCompact = true;
                }
                if (needCompact)
                    Compact();
            }
        }
    }
}
