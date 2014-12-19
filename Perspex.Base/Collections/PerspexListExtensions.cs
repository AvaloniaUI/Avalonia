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

    public static class PerspexListExtensions
    {
        public static IDisposable ForEachItem<T>(
            this IReadOnlyPerspexList<T> collection,
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

        public static IDisposable TrackItemPropertyChanged<T>(
            this IReadOnlyPerspexList<T> collection,
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
