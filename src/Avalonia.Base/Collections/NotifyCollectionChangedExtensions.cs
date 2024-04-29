using System;
using System.Collections.Specialized;
using Avalonia.Reactive;
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
            _ = collection ?? throw new ArgumentNullException(nameof(collection));

            return new WeakCollectionChangedObservable(new WeakReference<INotifyCollectionChanged>(collection));
        }

        /// <summary>
        /// Subscribes to the CollectionChanged event using a weak subscription.
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
            _ = collection ?? throw new ArgumentNullException(nameof(collection));
            _ = handler ?? throw new ArgumentNullException(nameof(handler));

            return collection.GetWeakCollectionChangedObservable()
                .Subscribe(e => handler(collection, e));
        }

        /// <summary>
        /// Subscribes to the CollectionChanged event using a weak subscription.
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
            _ = collection ?? throw new ArgumentNullException(nameof(collection));
            _ = handler ?? throw new ArgumentNullException(nameof(handler));

            return collection.GetWeakCollectionChangedObservable().Subscribe(handler);
        }

        private class WeakCollectionChangedObservable : LightweightObservableBase<NotifyCollectionChangedEventArgs>,
            IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
        {
            private readonly WeakReference<INotifyCollectionChanged> _sourceReference;

            public WeakCollectionChangedObservable(WeakReference<INotifyCollectionChanged> source)
            {
                _sourceReference = source;
            }

            public void OnEvent(object? sender,
                WeakEvent ev,
                NotifyCollectionChangedEventArgs e)
            {
                PublishNext(e);
            }
            protected override void Initialize()
            {
                if (_sourceReference.TryGetTarget(out var instance))
                    WeakEvents.CollectionChanged.Subscribe(instance, this);
            }

            protected override void Deinitialize()
            {
                if (_sourceReference.TryGetTarget(out var instance))
                    WeakEvents.CollectionChanged.Unsubscribe(instance, this);
            }
        }
    }
}
