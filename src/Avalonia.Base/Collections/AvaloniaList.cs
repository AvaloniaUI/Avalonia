// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Diagnostics;
using Avalonia.Platform;

namespace Avalonia.Collections
{
    /// <summary>
    /// Describes the action notified on a clear of a <see cref="AvaloniaList{T}"/>.
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
    /// AvaloniaList is similar to <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>
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
    public class AvaloniaList<T> : IAvaloniaList<T>, IList, INotifyCollectionChangedDebug
    {
        private List<T> _inner;
        private NotifyCollectionChangedEventHandler _collectionChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaList{T}"/> class.
        /// </summary>
        public AvaloniaList()
            : this(Enumerable.Empty<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaList{T}"/> class.
        /// </summary>
        /// <param name="items">The initial items for the collection.</param>
        public AvaloniaList(IEnumerable<T> items)
        {
            _inner = new List<T>(items);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaList{T}"/> class.
        /// </summary>
        /// <param name="items">The initial items for the collection.</param>
        public AvaloniaList(params T[] items)
        {
            _inner = new List<T>(items);
        }

        /// <summary>
        /// Raised when a change is made to the collection's items.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _collectionChanged += value; }
            remove { _collectionChanged -= value; }
        }

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

                if (!object.Equals(old, value))
                {
                    _inner[index] = value;

                    if (_collectionChanged != null)
                    {
                        var e = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            value,
                            old,
                            index);
                        _collectionChanged(this, e);
                    }
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
        public virtual void Add(T item)
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
        public virtual void AddRange(IEnumerable<T> items)
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
        public virtual void Clear()
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
        /// Gets a range of items from the collection.
        /// </summary>
        /// <param name="index">The first index to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        public IEnumerable<T> GetRange(int index, int count)
        {
            return _inner.GetRange(index, count);
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
        public virtual void Insert(int index, T item)
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
        public virtual void InsertRange(int index, IEnumerable<T> items)
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
        /// Moves an item to a new index.
        /// </summary>
        /// <param name="oldIndex">The index of the item to move.</param>
        /// <param name="newIndex">The index to move the item to.</param>
        public void Move(int oldIndex, int newIndex)
        {
            var item = this[oldIndex];
            _inner.RemoveAt(oldIndex);
            _inner.Insert(newIndex, item);

            if (_collectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Move,
                    item,
                    newIndex,
                    oldIndex);
                _collectionChanged(this, e);
            }
        }

        /// <summary>
        /// Moves multiple items to a new index.
        /// </summary>
        /// <param name="oldIndex">The first index of the items to move.</param>
        /// <param name="count">The number of items to move.</param>
        /// <param name="newIndex">The index to move the items to.</param>
        public void MoveRange(int oldIndex, int count, int newIndex)
        {
            var items = _inner.GetRange(oldIndex, count);
            var modifiedNewIndex = newIndex;
            _inner.RemoveRange(oldIndex, count);

            if (newIndex > oldIndex)
            {
                modifiedNewIndex -= count;
            }

            _inner.InsertRange(modifiedNewIndex, items);

            if (_collectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Move,
                    items,
                    newIndex,
                    oldIndex);
                _collectionChanged(this, e);
            }
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if the item was found and removed, otherwise false.</returns>
        public virtual bool Remove(T item)
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
        public virtual void RemoveAll(IEnumerable<T> items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

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
        public virtual void RemoveAt(int index)
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
        public virtual void RemoveRange(int index, int count)
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

        /// <inheritdoc/>
        Delegate[] INotifyCollectionChangedDebug.GetCollectionChangedSubscribers() => _collectionChanged?.GetInvocationList();

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with an add action.
        /// </summary>
        /// <param name="t">The items that were added.</param>
        /// <param name="index">The starting index.</param>
        private void NotifyAdd(IList t, int index)
        {
            if (_collectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, t, index);
                _collectionChanged(this, e);
            }

            NotifyCountChanged();
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event when the <see cref="Count"/> property
        /// changes.
        /// </summary>
        private void NotifyCountChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with a remove action.
        /// </summary>
        /// <param name="t">The items that were removed.</param>
        /// <param name="index">The starting index.</param>
        private void NotifyRemove(IList t, int index)
        {
            if (_collectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, index);
                _collectionChanged(this, e);
            }

            NotifyCountChanged();
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with a reset action.
        /// </summary>
        /// <param name="t">The items that were removed.</param>
        private void NotifyReset(IList t)
        {
            if (_collectionChanged != null)
            {
                NotifyCollectionChangedEventArgs e;

                e = ResetBehavior == ResetBehavior.Reset ? 
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) : 
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, t, 0);

                _collectionChanged(this, e);
            }

            NotifyCountChanged();
        }
    }
}