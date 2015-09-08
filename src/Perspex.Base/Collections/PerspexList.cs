// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Perspex.Collections
{
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
    public class PerspexList<T> : IPerspexList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private List<T> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexList{T}"/> class.
        /// </summary>
        public PerspexList()
            : this(Enumerable.Empty<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexList{T}"/> class.
        /// </summary>
        /// <param name="items">The initial items for the collection.</param>
        public PerspexList(IEnumerable<T> items)
        {
            _inner = new List<T>(items);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexList{T}"/> class.
        /// </summary>
        /// <param name="items">The initial items for the collection.</param>
        public PerspexList(params T[] items)
        {
            _inner = new List<T>(items);
        }

        /// <summary>
        /// Raised when a change is made to the collection's items.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raised when a property on the collection changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count
        {
            get { return _inner.Count; }
        }

        /// <inheritdoc/>
        bool IList.IsFixedSize
        {
            get { return false; }
        }

        /// <inheritdoc/>
        bool IList.IsReadOnly
        {
            get { return false; }
        }

        /// <inheritdoc/>
        int ICollection.Count
        {
            get { return _inner.Count; }
        }

        /// <inheritdoc/>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <inheritdoc/>
        object ICollection.SyncRoot
        {
            get { return null; }
        }

        /// <inheritdoc/>
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public T this[int index]
        {
            get
            {
                return _inner[index];
            }

            set
            {
                T old = _inner[index];
                _inner[index] = value;

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

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(T item)
        {
            int index = _inner.Count;
            _inner.Add(item);
            this.NotifyAdd(new[] { item }, index);
        }

        /// <summary>
        /// Adds multiple items to the collection.
        /// </summary>
        /// <param name="items">The items.</param>
        public void AddRange(IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            int index = _inner.Count;
            _inner.AddRange(items);
            this.NotifyAdd((items as IList) ?? items.ToList(), index);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            var old = _inner;
            _inner = new List<T>();
            this.NotifyRemove(old, 0);
        }

        /// <summary>
        /// Tests if the collection contains the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if the collection contains the item; otherwise false.</returns>
        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        /// <summary>
        /// Copies the collection's contents to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The first index of the array to copy to.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that enumerates the items in the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/>.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        /// <summary>
        /// Gets the index of the specified item in the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// The index of the item or -1 if the item is not contained in the collection.
        /// </returns>
        public int IndexOf(T item)
        {
            return _inner.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        public void Insert(int index, T item)
        {
            _inner.Insert(index, item);
            this.NotifyAdd(new[] { item }, index);
        }

        /// <summary>
        /// Inserts multiple items at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="items">The items.</param>
        public void InsertRange(int index, IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            _inner.InsertRange(index, items);
            this.NotifyAdd((items as IList) ?? items.ToList(), index);
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if the item was found and removed, otherwise false.</returns>
        public bool Remove(T item)
        {
            int index = _inner.IndexOf(item);

            if (index != -1)
            {
                _inner.RemoveAt(index);
                this.NotifyRemove(new[] { item }, index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes multiple items from the collection.
        /// </summary>
        /// <param name="items">The items.</param>
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

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            T item = _inner[index];
            _inner.RemoveAt(index);
            this.NotifyRemove(new[] { item }, index);
        }

        /// <inheritdoc/>
        int IList.Add(object value)
        {
            int index = this.Count;
            this.Add((T)value);
            return index;
        }

        /// <inheritdoc/>
        bool IList.Contains(object value)
        {
            return this.Contains((T)value);
        }

        /// <inheritdoc/>
        void IList.Clear()
        {
            this.Clear();
        }

        /// <inheritdoc/>
        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        /// <inheritdoc/>
        void IList.Insert(int index, object value)
        {
            this.Insert(index, (T)value);
        }

        /// <inheritdoc/>
        void IList.Remove(object value)
        {
            this.Remove((T)value);
        }

        /// <inheritdoc/>
        void IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index)
        {
            _inner.CopyTo((T[])array, index);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with an add action.
        /// </summary>
        /// <param name="t">The items that were added.</param>
        /// <param name="index">The starting index.</param>
        private void NotifyAdd(IList t, int index)
        {
            if (this.CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index);
                this.CollectionChanged(this, e);
            }

            this.NotifyCountChanged();
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event when the <see cref="Count"/> property
        /// changes.
        /// </summary>
        private void NotifyCountChanged()
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with a remove action.
        /// </summary>
        /// <param name="t">The items that were removed.</param>
        /// <param name="index">The starting index.</param>
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