using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
        /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber.</typeparam>
        /// <param name="target">The event source.</param>
        /// <param name="addHandler">Add handler selector.</param>
        /// <param name="removeHandler">Remove handler selector.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="subscriber">The subscriber.</param>
        public static void Subscribe<TTarget, TDelegate, TEventArgs, TSubscriber>(
            TTarget target,
            Action<TTarget, TDelegate> addHandler, Action<TTarget, TDelegate> removeHandler,
            string eventName,
            TDelegate subscriber)
            where TDelegate : Delegate
            where TEventArgs : EventArgs
            where TSubscriber : class
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            var dic = SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.GetOrCreateValue(target);

            if (!dic.TryGetValue(eventName, out var sub))
            {
                dic[eventName] = sub = new DelegateSubscription<TTarget, TDelegate, TEventArgs, TSubscriber>(
                    dic, addHandler, removeHandler, target, eventName);
            }

            sub.Add(subscriber);
        }

        /// <inheritdoc cref="Subscribe" />
        public static void Subscribe<TTarget, TEventArgs, TSubscriber>(
            TTarget target,
            Action<TTarget, EventHandler<TEventArgs>> addHandler, Action<TTarget, EventHandler<TEventArgs>> removeHandler,
            string eventName,
            EventHandler<TEventArgs> subscriber)
            where TEventArgs : EventArgs
            where TSubscriber : class
        {
            Subscribe<TTarget, EventHandler<TEventArgs>, TEventArgs, TSubscriber>(
                target, addHandler, removeHandler, eventName, subscriber);
        }

        /// <inheritdoc cref="Subscribe" />
        public static void Subscribe<TTarget, TSubscriber>(
            TTarget target,
            Action<TTarget, EventHandler> addHandler, Action<TTarget, EventHandler> removeHandler,
            string eventName,
            EventHandler subscriber)
            where TSubscriber : class
        {
            Subscribe<TTarget, EventHandler, EventArgs, TSubscriber>(
                target, addHandler, removeHandler, eventName, subscriber);
        }

        /// <inheritdoc cref="Subscribe" />
        [Obsolete("Don't use. Not AOT friendly.")]
        public static void Subscribe<TTarget, TEventArgs, TSubscriber>(TTarget target, string eventName, EventHandler<TEventArgs> subscriber)
            where TEventArgs : EventArgs where TSubscriber : class
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            var dic = SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.GetOrCreateValue(target);

            if (!dic.TryGetValue(eventName, out var sub))
            {
                dic[eventName] = sub = new ReflectionSubscription<TEventArgs, TSubscriber>(dic, typeof(TTarget), target, eventName);
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
            if (SubscriptionTypeStorage<TEventArgs, TSubscriber>.Subscribers.TryGetValue(target, out var dic))
            {
                if (dic.TryGetValue(eventName, out var sub))
                {
                    sub.Remove(subscriber);
                }
            }
        }

        /// <inheritdoc cref="Unsubscribe" />
        public static void Unsubscribe<TSubscriber>(object target, string eventName, EventHandler subscriber)
            where TSubscriber : class
        {
            if (SubscriptionTypeStorage<EventArgs, TSubscriber>.Subscribers.TryGetValue(target, out var dic))
            {
                if (dic.TryGetValue(eventName, out var sub))
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

        private class SubscriptionDic<T, TSubscriber> : Dictionary<string, SubscriptionBase<T, TSubscriber>>
            where T : EventArgs where TSubscriber : class
        {
        }

        private class DelegateSubscription<TTarget, TDelegate, T, TSubscriber> : SubscriptionBase<T, TSubscriber>
            where TDelegate : Delegate
            where T : EventArgs where TSubscriber : class
        {
            private readonly TTarget _target;
            private readonly Action<TTarget, TDelegate> _removeHandler;

            public DelegateSubscription(
                SubscriptionDic<T, TSubscriber> sdic,
                Action<TTarget, TDelegate> addHandler, Action<TTarget, TDelegate> removeHandler,
                TTarget target, string eventName)
                : base(sdic, typeof(TDelegate), eventName)
            {
                _target = target;
                _removeHandler = removeHandler;

                addHandler(target, (TDelegate)_delegate);
            }

            protected override void Destroy()
            {
                _removeHandler(_target, (TDelegate)_delegate);
                base.Destroy();
            }
        }

        [Obsolete("Don't use. Not AOT friendly.")]
        private class ReflectionSubscription<T, TSubscriber> : SubscriptionBase<T, TSubscriber>
            where T : EventArgs where TSubscriber : class
        {
            private readonly EventInfo _info;
            private readonly object _target;

            private static readonly Dictionary<Type, Dictionary<string, EventInfo>> Accessors = new();

            public ReflectionSubscription(SubscriptionDic<T, TSubscriber> sdic, Type targetType, object target, string eventName)
                : this(sdic, GetEvent(targetType, target, eventName), target, eventName)
            {

            }

            public ReflectionSubscription(SubscriptionDic<T, TSubscriber> sdic, EventInfo info, object target, string eventName)
                : base(sdic, info.EventHandlerType!, eventName)
            {
                _target = target;
                _info = info;

                _info.AddMethod!.Invoke(target, new[] { _delegate });
            }

            protected override void Destroy()
            {
                _info.RemoveMethod!.Invoke(_target, new[] { _delegate });
                base.Destroy();
            }

            private static EventInfo GetEvent(Type targetType, object target, string eventName)
            {
                if (!Accessors.TryGetValue(targetType, out var evDic))
                    Accessors[targetType] = evDic = new Dictionary<string, EventInfo>();

                if (evDic.TryGetValue(eventName, out var info))
                {
                    return info;
                }
                else
                {
                    var ev = targetType.GetRuntimeEvents().FirstOrDefault(x => x.Name == eventName);

                    if (ev == null)
                    {
                        throw new ArgumentException(
                            $"The event {eventName} was not found on {target.GetType()}.");
                    }

                    return evDic[eventName] = ev;
                }
            }
        }

        private abstract class SubscriptionBase<T, TSubscriber>
            where T : EventArgs where TSubscriber : class
        {
            private readonly SubscriptionDic<T, TSubscriber> _sdic;
            private readonly string _eventName;
            protected readonly Delegate _delegate;

            private Descriptor[] _data = new Descriptor[2];
            private int _count = 0;

            private delegate void CallerDelegate(TSubscriber s, object sender, T args);

            struct Descriptor
            {
                public WeakReference<TSubscriber> Subscriber;
                public CallerDelegate Caller;
            }

            public SubscriptionBase(SubscriptionDic<T, TSubscriber> sdic, Type delegateType, string eventName)
            {
                _sdic = sdic;
                _eventName = eventName;

                var del = new Action<object, T>(OnEvent);
                _delegate = del.GetMethodInfo().CreateDelegate(delegateType, del.Target);
            }

            private static Dictionary<MethodInfo, CallerDelegate> s_Callers =
                new Dictionary<MethodInfo, CallerDelegate>();

            protected virtual void Destroy()
            {
                _sdic.Remove(_eventName);
            }

            public void Add(Delegate s)
            {
                Compact(true);
                if (_count == _data.Length)
                {
                    //Extend capacity
                    var ndata = new Descriptor[_data.Length * 2];
                    Array.Copy(_data, ndata, _data.Length);
                    _data = ndata;
                }

                MethodInfo method = s.Method;

                var subscriber = (TSubscriber)s.Target!;
                if (!s_Callers.TryGetValue(method, out var caller))
                    s_Callers[method] = caller =
                        (CallerDelegate)Delegate.CreateDelegate(typeof(CallerDelegate), null, method);
                _data[_count] = new Descriptor
                {
                    Caller = caller,
                    Subscriber = new WeakReference<TSubscriber>(subscriber)
                };
                _count++;
            }

            public void Remove(Delegate s)
            {
                var removed = false;

                for (int c = 0; c < _count; ++c)
                {
                    var reference = _data[c].Subscriber;

                    if (reference != null && reference.TryGetTarget(out var instance) && Equals(instance, s.Target))
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

                    TSubscriber? target = null;

                    r.Subscriber?.TryGetTarget(out target);

                    //Mark current index as first empty
                    if (target == null && empty == -1)
                        empty = c;
                    //If current element isn't null and we have an empty one
                    if (target != null && empty != -1)
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

            protected void OnEvent(object sender, T eventArgs)
            {
                var needCompact = false;
                for (var c = 0; c < _count; c++)
                {
                    var r = _data[c].Subscriber;
                    if (r.TryGetTarget(out var sub))
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
