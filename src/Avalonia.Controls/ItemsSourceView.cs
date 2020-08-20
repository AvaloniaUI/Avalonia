// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a standardized view of the supported interactions between a given
    /// <see cref="ItemsControl"/> or <see cref="ItemsRepeater"/> and its items.
    /// </summary>
    public class ItemsSourceView<T> : INotifyCollectionChanged, IDisposable, IReadOnlyList<T>
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView{T}"/>
        /// </summary>
        public static ItemsSourceView<T> Empty { get; } = new ItemsSourceView<T>(Array.Empty<T>());

        private readonly IList _inner;
        private INotifyCollectionChanged? _notifyCollectionChanged;

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable<T> source)
            : this((IEnumerable)source)
        {
        }

        private protected ItemsSourceView(IEnumerable source)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));

            if (source is IList list)
            {
                _inner = list;
            }
            else if (source is IEnumerable<object?> enumerable)
            {
                _inner = new List<object?>(enumerable);
            }
            else
            {
                _inner = new List<object?>(source.Cast<object?>());
            }

            ListenToCollectionChanges();
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Gets a value that indicates whether the items source can provide a unique key for each item.
        /// </summary>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public bool HasKeyIndexMapping => false;

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public T this[int index] => GetAt(index);

        /// <summary>
        /// Occurs when the collection has changed to indicate the reason for the change and which items changed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_notifyCollectionChanged != null)
            {
                _notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public T GetAt(int index) => _inner is IList<T> typed ? typed[index] : (T)_inner[index];

        public int IndexOf(object? item) => _inner.IndexOf(item);

        public static ItemsSourceView<T> GetOrCreate(IEnumerable<T>? items)
        {
            if (items is ItemsSourceView<T> isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return Empty;
            }
            else
            {
                return new ItemsSourceView<T>(items);
            }
        }

        /// <summary>
        /// Retrieves the index of the item that has the specified unique identifier (key).
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The key</returns>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public string KeyFromIndex(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the unique identifier (key) for the item at the specified index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The index.</returns>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public int IndexFromKey(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() => _inner is IList<T> typed ?
            typed.GetEnumerator() : _inner.Cast<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        internal void AddListener(ICollectionChangedListener listener)
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.AddListener(incc, listener);
            }
        }

        internal void RemoveListener(ICollectionChangedListener listener)
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.RemoveListener(incc, listener);
            }
        }

        protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        private void ListenToCollectionChanges()
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnCollectionChanged;
                _notifyCollectionChanged = incc;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemsSourceChanged(e);
        }
    }

    public class ItemsSourceView : ItemsSourceView<object?>
    {
        public ItemsSourceView(IEnumerable source)
            : base(source)
        {
        }
    }
}
