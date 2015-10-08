// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;

namespace Perspex.Collections
{
    /// <summary>
    /// Defines extension methods for working with <see cref="PerspexList{T}"/>s.
    /// </summary>
    public static class PerspexListExtensions
    {
        /// <summary>
        /// Invokes an action for each item in a collection and subsequently each item added or
        /// removed from the collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="added">
        /// An action called initially for each item in the collection and subsequently for each
        /// item added to the collection. The parameters passed are the index in the collection and
        /// the item.
        /// </param>
        /// <param name="removed">
        /// An action called for each item removed from the collection. The parameters passed are
        /// the index in the collection and the item.
        /// </param>
        /// <param name="reset">
        /// An action called when the collection is reset.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable ForEachItem<T>(
            this IPerspexReadOnlyList<T> collection,
            Action<T> added,
            Action<T> removed,
            Action reset)
        {
            return collection.ForEachItem((_, i) => added(i), (_, i) => removed(i), reset);
        }

        /// <summary>
        /// Invokes an action for each item in a collection and subsequently each item added or
        /// removed from the collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="added">
        /// An action called initially for each item in the collection and subsequently for each
        /// item added to the collection. The parameters passed are the index in the collection and
        /// the item.
        /// </param>
        /// <param name="removed">
        /// An action called for each item removed from the collection. The parameters passed are
        /// the index in the collection and the item.
        /// </param>
        /// <param name="reset">
        /// An action called when the collection is reset.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable ForEachItem<T>(
            this IPerspexReadOnlyList<T> collection,
            Action<int, T> added,
            Action<int, T> removed,
            Action reset)
        {
            int index;

            NotifyCollectionChangedEventHandler handler = (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        index = e.NewStartingIndex;

                        foreach (T item in e.NewItems)
                        {
                            added(index++, item);
                        }

                        break;

                    case NotifyCollectionChangedAction.Replace:
                        index = e.OldStartingIndex;

                        foreach (T item in e.OldItems)
                        {
                            removed(index++, item);
                        }

                        index = e.NewStartingIndex;

                        foreach (T item in e.NewItems)
                        {
                            added(index++, item);
                        }

                        break;

                    case NotifyCollectionChangedAction.Remove:
                        index = e.OldStartingIndex;

                        foreach (T item in e.OldItems)
                        {
                            removed(index++, item);
                        }

                        break;

                    case NotifyCollectionChangedAction.Reset:
                        if (reset == null)
                        {
                            throw new InvalidOperationException(
                                "Reset called on collection without reset handler.");
                        }

                        reset();
                        break;
                }
            };

            index = 0;
            foreach (T i in collection)
            {
                added(index++, i);
            }

            collection.CollectionChanged += handler;

            return Disposable.Create(() => collection.CollectionChanged -= handler);
        }

        /// <summary>
        /// Invokes an action for each item in a collection and subsequently each item added or
        /// removed from the collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="added">
        /// An action called initially with all items in the collection and subsequently with a
        /// list of items added to the collection. The parameters passed are the index of the
        /// first item added to the collection and the items added.
        /// </param>
        /// <param name="removed">
        /// An action called with all items removed from the collection. The parameters passed 
        /// are the index of the first item removed from the collection and the items removed.
        /// </param>
        /// <param name="reset">
        /// An action called when the collection is reset.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable ForEachItem<T>(
            this IPerspexReadOnlyList<T> collection,
            Action<int, IEnumerable<T>> added,
            Action<int, IEnumerable<T>> removed,
            Action reset)
        {
            NotifyCollectionChangedEventHandler handler = (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        added(e.NewStartingIndex, e.NewItems.Cast<T>());
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        removed(e.OldStartingIndex, e.OldItems.Cast<T>());
                        added(e.NewStartingIndex, e.NewItems.Cast<T>());
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        removed(e.OldStartingIndex, e.OldItems.Cast<T>());
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        if (reset == null)
                        {
                            throw new InvalidOperationException(
                                "Reset called on collection without reset handler.");
                        }

                        reset();
                        break;
                }
            };

            added(0, collection);
            collection.CollectionChanged += handler;

            return Disposable.Create(() => collection.CollectionChanged -= handler);
        }

        /// <summary>
        /// Listens for property changed events from all items in a collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="callback">A callback to call for each property changed event.</param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable TrackItemPropertyChanged<T>(
            this IPerspexReadOnlyList<T> collection,
            Action<Tuple<object, PropertyChangedEventArgs>> callback)
        {
            List<INotifyPropertyChanged> tracked = new List<INotifyPropertyChanged>();

            PropertyChangedEventHandler handler = (s, e) =>
            {
                callback(Tuple.Create(s, e));
            };

            collection.ForEachItem(
                x =>
                {
                    var inpc = x as INotifyPropertyChanged;

                    if (inpc != null)
                    {
                        inpc.PropertyChanged += handler;
                        tracked.Add(inpc);
                    }
                },
                x =>
                {
                    var inpc = x as INotifyPropertyChanged;

                    if (inpc != null)
                    {
                        inpc.PropertyChanged -= handler;
                        tracked.Remove(inpc);
                    }
                },
                null);

            return Disposable.Create(() =>
            {
                foreach (var i in tracked)
                {
                    i.PropertyChanged -= handler;
                }
            });
        }
    }
}
