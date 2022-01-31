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

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a standardized view of the supported interactions between a given ItemsSource
    /// object and an <see cref="ItemsRepeater"/> control.
    /// </summary>
    /// <remarks>
    /// Components written to work with ItemsRepeater should consume the
    /// <see cref="ItemsRepeater.Items"/> via ItemsSourceView since this provides a normalized
    /// view of the Items. That way, each component does not need to know if the source is an
    /// IEnumerable, an IList, or something else.
    /// </remarks>
    public class ItemsSourceView : INotifyCollectionChanged, IDisposable
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public static ItemsSourceView Empty { get; } = new ItemsSourceView(Array.Empty<object>());

        private IList? _inner;
        private NotifyCollectionChangedEventHandler? _collectionChanged;

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable source)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            _inner = source switch
            {
                ItemsSourceView _ => throw new ArgumentException("Cannot wrap an existing ItemsSourceView.", nameof(source)),
                IList list => list,
                INotifyCollectionChanged _ => throw new ArgumentException(
                    "Collection implements INotifyCollectionChanged by not IList.",
                    nameof(source)),
                IEnumerable<object> iObj => new List<object>(iObj),
                _ => new List<object>(source.Cast<object>())
            };
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count => Inner.Count;

        /// <summary>
        /// Gets a value that indicates whether the items source can provide a unique key for each item.
        /// </summary>
        /// <remarks>
        /// TODO: Not yet implemented in Avalonia.
        /// </remarks>
        public bool HasKeyIndexMapping => false;

        /// <summary>
        /// Gets the inner collection.
        /// </summary>
        public IList Inner
        {
            get
            {
                if (_inner is null)
                    ThrowDisposed();
                return _inner!;
            }
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public object? this[int index] => GetAt(index);

        /// <summary>
        /// Occurs when the collection has changed to indicate the reason for the change and which items changed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                if (_collectionChanged is null && Inner is INotifyCollectionChanged incc)
                {
                    incc.CollectionChanged += OnCollectionChanged;
                }

                _collectionChanged += value;
            }

            remove
            {
                _collectionChanged -= value;

                if (_collectionChanged is null && Inner is INotifyCollectionChanged incc)
                {
                    incc.CollectionChanged -= OnCollectionChanged;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= OnCollectionChanged;
            }

            _inner = null;
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public object? GetAt(int index) => Inner[index];

        public int IndexOf(object? item) => Inner.IndexOf(item);

        public static ItemsSourceView GetOrCreate(IEnumerable? items)
        {
            if (items is ItemsSourceView isv)
            {
                return isv;
            }
            else if (items is null)
            {
                return Empty;
            }
            else
            {
                return new ItemsSourceView(items);
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

        internal void AddListener(ICollectionChangedListener listener)
        {
            if (Inner is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.AddListener(incc, listener);
            }
        }

        internal void RemoveListener(ICollectionChangedListener listener)
        {
            if (Inner is INotifyCollectionChanged incc)
            {
                CollectionChangedEventManager.Instance.RemoveListener(incc, listener);
            }
        }

        protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
        {
            _collectionChanged?.Invoke(this, args);
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemsSourceChanged(e);
        }

        private void ThrowDisposed() => throw new ObjectDisposedException(nameof(ItemsSourceView));
    }

    public class ItemsSourceView<T> : ItemsSourceView, IReadOnlyList<T>
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public new static ItemsSourceView<T> Empty { get; } = new ItemsSourceView<T>(Array.Empty<T>());

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        public ItemsSourceView(IEnumerable<T> source)
            : base(source)
        {
        }

        private ItemsSourceView(IEnumerable source)
            : base(source)
        {
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
#pragma warning disable CS8603
        public new T this[int index] => GetAt(index);
#pragma warning restore CS8603

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public new T GetAt(int index) => (T)Inner[index]!;

        public IEnumerator<T> GetEnumerator() => Inner.Cast<T>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();

        public static new ItemsSourceView<T> GetOrCreate(IEnumerable? items)
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
    }
}
