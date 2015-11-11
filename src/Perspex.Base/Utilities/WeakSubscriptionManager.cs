using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Utilities
{
    public static class WeakSubscriptionManager
    {
        static class SubscriptionTypeStorage<T>
        {
            public static readonly ConditionalWeakTable<object, SubscriptionDic<T>> Subscribers
                = new ConditionalWeakTable<object, SubscriptionDic<T>>();
        }

        class SubscriptionDic<T> : Dictionary<string, Subscription<T>>
        {

        }



        static readonly Dictionary<Type, Dictionary<string, EventInfo>> Accessors
            = new Dictionary<Type, Dictionary<string, EventInfo>>();

        class Subscription<T>
        {
            WeakReference<IWeakSubscriber<T>>[] _data = new WeakReference<IWeakSubscriber<T>>[16];
            int _count = 0;

            readonly EventInfo _info;
            readonly SubscriptionDic<T> _sdic;
            private readonly object _target;
            private readonly string _eventName;
            private readonly EventHandler<T> _delegate;
            public Subscription(SubscriptionDic<T> sdic, object target, string eventName)
            {
                _sdic = sdic;
                _target = target;
                _eventName = eventName;
                var t = target.GetType();
                Dictionary<string, EventInfo> evDic;
                if (!Accessors.TryGetValue(t, out evDic))
                    Accessors[t] = evDic = new Dictionary<string, EventInfo>();
                if (!evDic.TryGetValue(eventName, out _info))
                    evDic[eventName] = _info = t.GetRuntimeEvent(eventName);
                _info.AddEventHandler(target, _delegate = OnEvent);
            }

            void Destroy()
            {
                _info.RemoveEventHandler(_target, _delegate);
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

            void Compact()
            {
                int empty = -1;
                for (int c = 1; c < _count; c++)
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

        public static void Subscribe<T>(object target, string eventName, IWeakSubscriber<T> subscriber)
        {
            var dic = SubscriptionTypeStorage<T>.Subscribers.GetOrCreateValue(target);
            Subscription<T> sub;
            if (!dic.TryGetValue(eventName, out sub))
                dic[eventName] = sub = new Subscription<T>(dic, target, eventName);
            sub.Add(new WeakReference<IWeakSubscriber<T>>(subscriber));
        }
    }

    public interface IWeakSubscriber<T>
    {
        void OnEvent(object sender, T ev);
    }
}
