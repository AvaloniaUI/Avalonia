// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
    public static class WeakEventHandlerManager
    {
        /// <summary>
        /// Subscribes to an event on an object using a weak subscription.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber.</typeparam>
        /// <param name="target">The event source.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="subscriber">The subscriber.</param>
        public static void Subscribe<TTarget, TEventArgs, TSubscriber>(TTarget target, string eventName, EventHandler<TEventArgs> subscriber)
            where TEventArgs : EventArgs where TSubscriber : class
        {
            var dic = SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.GetOrCreateValue(target);
            Subscription<TEventArgs, TSubscriber> sub;

            if (!dic.TryGetValue(eventName, out sub))
            {
                dic[eventName] = sub = new Subscription<TEventArgs, TSubscriber>(dic, typeof(TTarget), target, eventName);
            }

            sub.Add(subscriber);
        }

        /// <summary>
        /// Unsubscribes from an event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber.</typeparam>
        /// <param name="target">The event source.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="subscriber">The subscriber.</param>
        public static void Unsubscribe<TEventArgs, TSubscriber>(object target, string eventName, EventHandler<TEventArgs> subscriber)
            where TEventArgs : EventArgs where TSubscriber : class
        {
            SubscriptionDic<TEventArgs, TSubscriber> dic;

            if (SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.TryGetValue(target, out dic))
            {
                Subscription<TEventArgs, TSubscriber> sub;

                if (dic.TryGetValue(eventName, out sub))
                {
                    sub.Remove(subscriber);
                }
            }
        }

        private static class SubscriptionTypeStorage<TArgs, TSubscriber>
            where TArgs : EventArgs where TSubscriber : class
        {
            public static readonly ConditionalWeakTable<object, SubscriptionDic<TArgs, TSubscriber>> Subscribers
                = new ConditionalWeakTable<object, SubscriptionDic<TArgs, TSubscriber>>();
        }

        private class SubscriptionDic<T, TSubscriber> : Dictionary<string, Subscription<T, TSubscriber>>
            where T : EventArgs where TSubscriber : class
        {
        }

        private static readonly Dictionary<Type, Dictionary<string, EventInfo>> Accessors
            = new Dictionary<Type, Dictionary<string, EventInfo>>();

        private class Subscription<T, TSubscriber> where T : EventArgs where TSubscriber : class
        {
            private readonly EventInfo _info;
            private readonly SubscriptionDic<T, TSubscriber> _sdic;
            private readonly object _target;
            private readonly string _eventName;
            private readonly Delegate _delegate;

            private Descriptor[] _data = new Descriptor[2];
            private int _count = 0;

            delegate void CallerDelegate(TSubscriber s, object sender, T args);
            
            struct Descriptor
            {
                public WeakReference<TSubscriber> Subscriber;
                public CallerDelegate Caller;
            }

            private static Dictionary<MethodInfo, CallerDelegate> s_Callers =
                new Dictionary<MethodInfo, CallerDelegate>();
            
            public Subscription(SubscriptionDic<T, TSubscriber> sdic, Type targetType, object target, string eventName)
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

            public void Add(EventHandler<T> s)
            {
                Compact(true);
                if (_count == _data.Length)
                {
                    //Extend capacity
                    var ndata = new Descriptor[_data.Length*2];
                    Array.Copy(_data, ndata, _data.Length);
                    _data = ndata;
                }

                var subscriber = (TSubscriber)s.Target;
                if (!s_Callers.TryGetValue(s.Method, out var caller))
                    s_Callers[s.Method] = caller =
                        (CallerDelegate)Delegate.CreateDelegate(typeof(CallerDelegate), null, s.Method);
                _data[_count] = new Descriptor
                {
                    Caller = caller,
                    Subscriber = new WeakReference<TSubscriber>(subscriber)
                };
                _count++;
            }

            public void Remove(EventHandler<T> s)
            {
                var removed = false;

                for (int c = 0; c < _count; ++c)
                {
                    var reference = _data[c].Subscriber;
                    TSubscriber instance;

                    if (reference != null && reference.TryGetTarget(out instance) && instance == s)
                    {
                        _data[c] = default;
                        removed = true;
                    }
                }

                if (removed)
                {
                    Compact();
                }
            }

            void Compact(bool preventDestroy = false)
            {
                int empty = -1;
                for (int c = 0; c < _count; c++)
                {
                    var r = _data[c];
                    //Mark current index as first empty
                    if (r.Subscriber == null && empty == -1)
                        empty = c;
                    //If current element isn't null and we have an empty one
                    if (r.Subscriber != null && empty != -1)
                    {
                        _data[c] = default;
                        _data[empty] = r;
                        empty++;
                    }
                }
                if (empty != -1)
                    _count = empty;
                if (_count == 0 && !preventDestroy)
                    Destroy();
            }

            void OnEvent(object sender, T eventArgs)
            {
                var needCompact = false;
                for(var c=0; c<_count; c++)
                {
                    var r = _data[c].Subscriber;
                    TSubscriber sub;
                    if (r.TryGetTarget(out sub))
                    {
                        _data[c].Caller(sub, sender, eventArgs);
                    }
                    else
                        needCompact = true;
                }
                if (needCompact)
                    Compact();
            }
        }
    }
}
