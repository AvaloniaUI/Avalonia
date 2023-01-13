using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Reactive;

namespace Avalonia.Collections
{
    /// <summary>
    /// Defines extension methods for working with <see cref="AvaloniaList{T}"/>s.
    /// </summary>
    public static class AvaloniaListExtensions
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
        /// <param name="weakSubscription">
        /// Indicates if a weak subscription should be used to track changes to the collection.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable ForEachItem<T>(
            this IAvaloniaReadOnlyList<T> collection,
            Action<T> added,
            Action<T> removed,
            Action reset,
            bool weakSubscription = false)
        {
            return collection.ForEachItem((_, i) => added(i), (_, i) => removed(i), reset, weakSubscription);
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
        /// An action called when the collection is reset. This will be followed by calls to 
        /// <paramref name="added"/> for each item present in the collection after the reset.
        /// </param>
        /// <param name="weakSubscription">
        /// Indicates if a weak subscription should be used to track changes to the collection.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable ForEachItem<T>(
            this IAvaloniaReadOnlyList<T> collection,
            Action<int, T> added,
            Action<int, T> removed,
            Action reset,
            bool weakSubscription = false)
        {
            void Add(int index, IList items)
            {
                foreach (T item in items)
                {
                    added(index++, item);
                }
            }

            void Remove(int index, IList items)
            {
                for (var i = items.Count - 1; i >= 0; --i)
                {
                    removed(index + i, (T)items[i]!);
                }
            }

            NotifyCollectionChangedEventHandler handler = (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Add(e.NewStartingIndex, e.NewItems!);
                        break;

                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                        Remove(e.OldStartingIndex, e.OldItems!);
                        int newIndex = e.NewStartingIndex;
                        if(newIndex > e.OldStartingIndex)
                        {
                            newIndex -= e.OldItems!.Count;
                        }
                        Add(newIndex, e.NewItems!);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        Remove(e.OldStartingIndex, e.OldItems!);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        if (reset == null)
                        {
                            throw new InvalidOperationException(
                                "Reset called on collection without reset handler.");
                        }

                        reset();
                        Add(0, (IList)collection);
                        break;
                }
            };

            Add(0, (IList)collection);

            if (weakSubscription)
            {
                return collection.WeakSubscribe(handler);
            }
            else
            {
                collection.CollectionChanged += handler;

                return Disposable.Create(() => collection.CollectionChanged -= handler);
            }
        }

        /// <summary>
        /// Listens for property changed events from all items in a collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="callback">A callback to call for each property changed event.</param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable TrackItemPropertyChanged<T>(
            this IAvaloniaReadOnlyList<T> collection,
            Action<Tuple<object?, PropertyChangedEventArgs>> callback)
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
                () => throw new NotSupportedException("Collection reset not supported."));

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
