// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Provides extension methods for working with weak event handlers.
    /// </summary>
    public static class WeakObservable
    {
        /// <summary>
        /// Converts a .NET event conforming to the standard .NET event pattern into an observable
        /// sequence, subscribing weakly.
        /// </summary>
        /// <typeparam name="TTarget">The type of target.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event args.</typeparam>
        /// <param name="target">Object instance that exposes the event to convert.</param>
        /// <param name="eventName">Name of the event to convert.</param>
        /// <returns></returns>
        public static IObservable<EventPattern<object, TEventArgs>> FromEventPattern<TTarget, TEventArgs>(
            TTarget target, 
            string eventName)
            where TEventArgs : EventArgs
        {
            Contract.Requires<ArgumentNullException>(target != null);
            Contract.Requires<ArgumentNullException>(eventName != null);

            return Observable.Create<EventPattern<object, TEventArgs>>(observer =>
            {
                var handler = new Handler<TEventArgs>(observer);
                WeakSubscriptionManager.Subscribe(target, eventName, handler);
                return () => WeakSubscriptionManager.Unsubscribe(target, eventName, handler);
            }).Publish().RefCount();
        }

        private class Handler<TEventArgs> : IWeakSubscriber<TEventArgs> where TEventArgs : EventArgs
        {
            private IObserver<EventPattern<object, TEventArgs>> _observer;

            public Handler(IObserver<EventPattern<object, TEventArgs>> observer)
            {
                _observer = observer;
            }

            public void OnEvent(object sender, TEventArgs e)
            {
                _observer.OnNext(new EventPattern<object, TEventArgs>(sender, e));
            }
        }
    }
}
