// -----------------------------------------------------------------------
// <copyright file="PerspexList.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Reactive.Linq;

    public class PerspexList<T> : ObservableCollection<T>
    {
        public PerspexList()
        {
            this.Initialize();
        }

        public PerspexList(IEnumerable<T> items)
            : base(items)
        {
            this.Initialize();
        }

        public IObservable<NotifyCollectionChangedEventArgs> Changed
        {
            get;
            private set;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                this.Add(item);
            }
        }

        private void Initialize()
        {
            this.Changed = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => (sender, e) => handler(e),
                handler => this.CollectionChanged += handler,
                handler => this.CollectionChanged -= handler);
        }
    }
}