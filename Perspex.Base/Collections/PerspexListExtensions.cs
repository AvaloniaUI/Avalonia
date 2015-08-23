// -----------------------------------------------------------------------
// <copyright file="PerspexListExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Reactive.Disposables;

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
        /// item added to the collection.
        /// </param>
        /// <param name="removed">
        /// An action called for each item removed from the collection.
        /// </param>
        /// <returns>A disposable used to terminate the subscription.</returns>
        public static IDisposable ForEachItem<T>(
            this IPerspexReadOnlyList<T> collection,
            Action<T> added,
            Action<T> removed)
        {
            NotifyCollectionChangedEventHandler handler = (_, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (T i in e.NewItems)
                        {
                            added(i);
                        }

                        break;

                    case NotifyCollectionChangedAction.Replace:
                        foreach (T i in e.OldItems)
                        {
                            removed(i);
                        }

                        foreach (T i in e.NewItems)
                        {
                            added(i);
                        }

                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (T i in e.OldItems)
                        {
                            removed(i);
                        }

                        break;
                }
            };

            foreach (T i in collection)
            {
                added(i);
            }

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
                });

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
