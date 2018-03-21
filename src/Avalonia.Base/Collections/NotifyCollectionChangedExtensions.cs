// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Utilities;

namespace Avalonia.Collections
{
    public static class NotifyCollectionChangedExtensions
    {
        /// <summary>
        /// Gets a weak observable for the CollectionChanged event.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>An observable.</returns>
        public static IObservable<NotifyCollectionChangedEventArgs> GetWeakCollectionChangedObservable(
            this INotifyCollectionChanged collection)
        {
            Contract.Requires<ArgumentNullException>(collection != null);

            return new WeakCollectionChangedObservable(new WeakReference<INotifyCollectionChanged>(collection));
        }

        /// <summary>
        /// Subcribes to the CollectionChanged event using a weak subscription.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="handler">
        /// An action called when the collection event is raised.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable WeakSubscribe(
            this INotifyCollectionChanged collection, 
            NotifyCollectionChangedEventHandler handler)
        {
            Contract.Requires<ArgumentNullException>(collection != null);
            Contract.Requires<ArgumentNullException>(handler != null);

            return
                collection.GetWeakCollectionChangedObservable()
                          .Subscribe(e => handler.Invoke(collection, e));
        }

        /// <summary>
        /// Subcribes to the CollectionChanged event using a weak subscription.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="handler">
        /// An action called when the collection event is raised.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable WeakSubscribe(
            this INotifyCollectionChanged collection,
            Action<NotifyCollectionChangedEventArgs> handler)
        {
            Contract.Requires<ArgumentNullException>(collection != null);
            Contract.Requires<ArgumentNullException>(handler != null);

            return
                collection.GetWeakCollectionChangedObservable()
                          .Subscribe(handler);
        }

        private class WeakCollectionChangedObservable : ObservableBase<NotifyCollectionChangedEventArgs>,
            IWeakSubscriber<NotifyCollectionChangedEventArgs>
        {
            private WeakReference<INotifyCollectionChanged> _sourceReference;
            private readonly Subject<NotifyCollectionChangedEventArgs> _changed = new Subject<NotifyCollectionChangedEventArgs>();

            private int _count;

            public WeakCollectionChangedObservable(WeakReference<INotifyCollectionChanged> source)
            {
                _sourceReference = source;
            }

            public void OnEvent(object sender, NotifyCollectionChangedEventArgs e)
            {
                _changed.OnNext(e);
            }

            protected override IDisposable SubscribeCore(IObserver<NotifyCollectionChangedEventArgs> observer)
            {
                if (_sourceReference.TryGetTarget(out INotifyCollectionChanged instance))
                {
                    if (_count++ == 0)
                    {
                        WeakSubscriptionManager.Subscribe(
                            instance,
                            nameof(instance.CollectionChanged),
                            this);
                    }

                    return Observable.Using(() => Disposable.Create(DecrementCount), _ => _changed)
                        .Subscribe(observer);
                }
                else
                {
                    _changed.OnCompleted();
                    observer.OnCompleted();
                    return Disposable.Empty;
                }
            }

            private void DecrementCount()
            {
                if (--_count == 0)
                {
                    if (_sourceReference.TryGetTarget(out INotifyCollectionChanged instance))
                    {
                        WeakSubscriptionManager.Unsubscribe(
                            instance,
                            nameof(instance.CollectionChanged),
                            this);
                    }
                }
            }
        }
    }
}
