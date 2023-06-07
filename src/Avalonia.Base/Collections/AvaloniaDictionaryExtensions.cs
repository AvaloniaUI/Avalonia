using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Reactive;

namespace Avalonia.Collections
{
    /// <summary>
    /// Defines extension methods for working with <see cref="AvaloniaList{T}"/>s.
    /// </summary>
    public static class AvaloniaDictionaryExtensions
    {
        /// <summary>
        /// Invokes an action for each item in a collection and subsequently each item added or
        /// removed from the collection.
        /// </summary>
        /// <typeparam name="TKey">The key type of the collection items.</typeparam>
        /// <typeparam name="TValue">The value type of the collection items.</typeparam>
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
        public static IDisposable ForEachItem<TKey, TValue>(
            this IAvaloniaReadOnlyDictionary<TKey, TValue> collection,
            Action<TKey, TValue> added,
            Action<TKey, TValue> removed,
            Action reset,
            bool weakSubscription = false)
            where TKey : notnull
        {
            void Add(IEnumerable items)
            {
                foreach (KeyValuePair<TKey, TValue> pair in items)
                {
                    added(pair.Key, pair.Value);
                }
            }

            void Remove(IEnumerable items)
            {
                foreach (KeyValuePair<TKey, TValue> pair in items)
                {
                    removed(pair.Key, pair.Value);
                }
            }

            NotifyCollectionChangedEventHandler handler = (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Add(e.NewItems!);
                        break;

                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                        Remove(e.OldItems!);
                        int newIndex = e.NewStartingIndex;
                        if(newIndex > e.OldStartingIndex)
                        {
                            newIndex -= e.OldItems!.Count;
                        }
                        Add(e.NewItems!);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        Remove(e.OldItems!);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        if (reset == null)
                        {
                            throw new InvalidOperationException(
                                "Reset called on collection without reset handler.");
                        }

                        reset();
                        Add(collection);
                        break;
                }
            };

            Add(collection);

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
    }
}
