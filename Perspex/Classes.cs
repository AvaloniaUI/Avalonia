// -----------------------------------------------------------------------
// <copyright file="Classes.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Subjects;

    public class Classes : ICollection<string>, INotifyCollectionChanged
    {
        private List<string> inner;

        private Subject<NotifyCollectionChangedEventArgs> beforeChanged
            = new Subject<NotifyCollectionChangedEventArgs>();

        private Subject<NotifyCollectionChangedEventArgs> changed
            = new Subject<NotifyCollectionChangedEventArgs>();

        private Subject<NotifyCollectionChangedEventArgs> afterChanged
            = new Subject<NotifyCollectionChangedEventArgs>();

        public Classes()
        {
            this.inner = new List<string>();
        }

        public Classes(params string[] classes)
        {
            this.inner = new List<string>(classes);
        }

        public Classes(IEnumerable<string> classes)
        {
            this.inner = new List<string>(classes);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count
        {
            get { return this.inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IObservable<NotifyCollectionChangedEventArgs> BeforeChanged
        {
            get { return this.beforeChanged; }
        }

        public IObservable<NotifyCollectionChangedEventArgs> Changed
        {
            get { return this.changed; }
        }

        public IObservable<NotifyCollectionChangedEventArgs> AfterChanged
        {
            get { return this.afterChanged; }
        }

        public void Add(string item)
        {
            this.Add(Enumerable.Repeat(item, 1));
        }

        public void Add(params string[] items)
        {
            this.Add((IEnumerable<string>)items);
        }

        public void Add(IEnumerable<string> items)
        {
            items = items.Except(this.inner);

            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                items);

            this.beforeChanged.OnNext(e);
            this.inner.AddRange(items);
            this.RaiseChanged(e);
        }

        private void RaiseChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }

            this.changed.OnNext(e);
            this.afterChanged.OnNext(e);
        }

        public void Clear()
        {
            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset);

            this.beforeChanged.OnNext(e);
            this.inner.Clear();
            this.RaiseChanged(e);
        }

        public bool Contains(string item)
        {
            return this.inner.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            this.inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        public bool Remove(string item)
        {
            return this.Remove(Enumerable.Repeat(item, 1));
        }

        public bool Remove(params string[] items)
        {
            return this.Remove((IEnumerable<string>)items);
        }

        public bool Remove(IEnumerable<string> items)
        {
            items = items.Intersect(this.inner);

            if (items.Any())
            {
                NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    items);

                this.beforeChanged.OnNext(e);

                foreach (string item in items)
                {
                    this.inner.Remove(item);
                }

                this.RaiseChanged(e);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
