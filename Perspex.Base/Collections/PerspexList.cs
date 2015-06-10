// -----------------------------------------------------------------------
// <copyright file="PerspexList.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// A notifying list.
    /// </summary>
    /// <typeparam name="T">The type of the list items.</typeparam>
    /// <remarks>
    /// PerspexList is similar to <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>
    /// except that when the <see cref="Clear"/> method is called, it notifies with a
    /// <see cref="NotifyCollectionChangedAction.Remove"/> action, passing the items that were
    /// removed.
    /// </remarks>
    public class PerspexList<T> : IPerspexList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private List<T> inner;

        public PerspexList()
            : this(Enumerable.Empty<T>())
        {
        }

        public PerspexList(IEnumerable<T> items)
        {
            this.inner = new List<T>(items);
        }

        public PerspexList(params T[] items)
        {
            this.inner = new List<T>(items);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count
        {
            get { return this.inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
       }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        int ICollection.Count
        {
            get { return this.inner.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return null; }
        }

        public T this[int index]
        {
            get
            {
                return this.inner[index];
            }

            set
            {
                T old = this.inner[index];
                this.inner[index] = value;

                if (this.CollectionChanged != null)
                {
                    var e = new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        value,
                        old);
                    this.CollectionChanged(this, e);
                }
            }
        }

        object IList.this[int index]
        {
            get { return this[index]; }

            set { this[index] = (T)value; }
        }

        public void Add(T item)
        {
            int index = this.inner.Count;
            this.inner.Add(item);
            this.NotifyAdd(new[] { item }, index);
        }

        public void AddRange(IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            int index = this.inner.Count;
            this.inner.AddRange(items);
            this.NotifyAdd((items as IList) ?? items.ToList(), index);
        }

        public void Clear()
        {
            var old = this.inner;
            this.inner = new List<T>();
            this.NotifyRemove(old, 0);
        }

        public bool Contains(T item)
        {
            return this.inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return this.inner.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.inner.Insert(index, item);
            this.NotifyAdd(new[] { item }, index);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            this.inner.InsertRange(index, items);
            this.NotifyAdd((items as IList) ?? items.ToList(), index);
        }

        public bool Remove(T item)
        {
            int index = this.inner.IndexOf(item);

            if (index != -1)
            {
                this.inner.RemoveAt(index);
                this.NotifyRemove(new[] { item }, index);
                return true;
            }

            return false;
        }

        public void RemoveAll(IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            List<T> removed = new List<T>();

            foreach (var i in items)
            {
                // TODO: Optimize to only send as many notifications as necessary.
                this.Remove(i);
            }
        }

        public void RemoveAt(int index)
        {
            T item = this.inner[index];
            this.inner.RemoveAt(index);
            this.NotifyRemove(new[] { item }, index);
        }

        int IList.Add(object value)
        {
            int index = this.Count;
            this.Add((T)value);
            return index;
        }

        bool IList.Contains(object value)
        {
            return this.Contains((T)value);
        }

        void IList.Clear()
        {
            this.Clear();
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            this.Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            this.Remove((T)value);
        }

        void IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.inner.CopyTo((T[])array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        private void NotifyAdd(IList t, int index)
        {
            if (this.CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index);
                this.CollectionChanged(this, e);
            }

            this.NotifyCountChanged();
        }

        private void NotifyCountChanged()
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        private void NotifyRemove(IList t, int index)
        {
            if (this.CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, index);
                this.CollectionChanged(this, e);
            }

            this.NotifyCountChanged();
        }
    }
}