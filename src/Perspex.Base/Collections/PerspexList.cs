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
    /// Describes the action notified on a clear of a <see cref="PerspexList{T}"/>.
    /// </summary>
    public enum ResetBehavior
    {
        /// <summary>
        /// Clearing the list notifies a with a 
        /// <see cref="NotifyCollectionChangedAction.Reset"/>.
        /// </summary>
        Reset,

        /// <summary>
        /// Clearing the list notifies a with a
        /// <see cref="NotifyCollectionChangedAction.Remove"/>.
        /// </summary>
        Remove,
    }

    /// <summary>
    /// A notifying list.
    /// </summary>
    /// <typeparam name="T">The type of the list items.</typeparam>
    /// <remarks>
    /// <para>
    /// PerspexList is similar to <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>
    /// with a few added features:
    /// </para>
    /// 
    /// <list type="bullet">
    /// <item>
    /// It can be configured to notify the <see cref="CollectionChanged"/> event with a
    /// <see cref="NotifyCollectionChangedAction.Remove"/> action instead of a
    /// <see cref="NotifyCollectionChangedAction.Reset"/> when the list is cleared by
    /// setting <see cref="ResetBehavior"/> to <see cref="ResetBehavior.Remove"/>.
    /// removed
    /// </item>
    /// <item>
    /// A <see cref="Validate"/> function can be used to validate each item before insertion.
    /// removed
    /// </item>
    /// </list>
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
        public int Count => _inner.Count;

        /// <summary>
        /// Gets or sets the reset behavior of the list.
        /// </summary>
        public ResetBehavior ResetBehavior { get; set; }

        /// <summary>
        /// Gets or sets a validation routine that can be used to validate items before they are
        /// added.
        /// </summary>
        public Action<T> Validate { get; set; }

        /// <inheritdoc/>
        bool IList.IsFixedSize => false;

        /// <inheritdoc/>
        bool IList.IsReadOnly => false;

        /// <inheritdoc/>
        int ICollection.Count => _inner.Count;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => null;

        /// <inheritdoc/>
        bool ICollection<T>.IsReadOnly => false;

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
                Validate?.Invoke(value);

                T old = _inner[index];
                _inner[index] = value;

                if (CollectionChanged != null)
                {
                    var e = new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        value,
                        old);
                    CollectionChanged(this, e);
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
            Validate?.Invoke(item);
            int index = _inner.Count;
            _inner.Add(item);
            NotifyAdd(new[] { item }, index);
        }

        /// <summary>
        /// Adds multiple items to the collection.
        /// </summary>
        /// <param name="items">The items.</param>
        public void AddRange(IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            var list = (items as IList) ?? items.ToList();

            if (list.Count > 0)
            {
                if (Validate != null)
                {
                    foreach (var item in list)
                    {
                        Validate((T)item);
                    }
                }

                int index = _inner.Count;
                _inner.AddRange(items);
                NotifyAdd(list, index);
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            if (this.Count > 0)
            {
                var old = _inner;
                _inner = new List<T>();
                NotifyReset(old);
            }
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
            Validate?.Invoke(item);
            _inner.Insert(index, item);
            NotifyAdd(new[] { item }, index);
        }

        /// <summary>
        /// Inserts multiple items at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="items">The items.</param>
        public void InsertRange(int index, IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            var list = (items as IList) ?? items.ToList();

            if (list.Count > 0)
            {
                if (Validate != null)
                {
                    foreach (var item in list)
                    {
                        Validate((T)item);
                    }
                }

                _inner.InsertRange(index, items);
                NotifyAdd((items as IList) ?? items.ToList(), index);
            }
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
                NotifyRemove(new[] { item }, index);
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

            var list = (items as IList) ?? items.ToList();

            foreach (var i in items)
            {
                // TODO: Optimize to only send as many notifications as necessary.
                Remove(i);
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
            NotifyRemove(new[] { item }, index);
        }

        /// <summary>
        /// Removes a range of elements from the collection.
        /// </summary>
        /// <param name="index">The first index to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                var list = _inner.GetRange(index, count);
                _inner.RemoveRange(index, count);
                NotifyRemove(list, index);
            }
        }

        /// <inheritdoc/>
        int IList.Add(object value)
        {
            int index = Count;
            Add((T)value);
            return index;
        }

        /// <inheritdoc/>
        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        /// <inheritdoc/>
        void IList.Clear()
        {
            Clear();
        }

        /// <inheritdoc/>
        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        /// <inheritdoc/>
        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        /// <inheritdoc/>
        void IList.Remove(object value)
        {
            Remove((T)value);
        }

        /// <inheritdoc/>
        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
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
            if (CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index);
                CollectionChanged(this, e);
            }

            NotifyCountChanged();
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event when the <see cref="Count"/> property
        /// changes.
        /// </summary>
        private void NotifyCountChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            }
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with a remove action.
        /// </summary>
        /// <param name="t">The items that were removed.</param>
        /// <param name="index">The starting index.</param>
        private void NotifyRemove(IList t, int index)
        {
            if (CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, index);
                CollectionChanged(this, e);
            }

            NotifyCountChanged();
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with a reset action.
        /// </summary>
        /// <param name="t">The items that were removed.</param>
        private void NotifyReset(IList t)
        {
            if (CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs e;

                if (ResetBehavior == ResetBehavior.Reset)
                {
                    e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                }
                else
                {
                    e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, 0);
                }

                CollectionChanged(this, e);
            }

            NotifyCountChanged();
        }
    }
}