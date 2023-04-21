// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a standardized view of the supported interactions between an items collection
    /// and an items control.
    /// </summary>
    public class ItemsSourceView : IReadOnlyList<object?>,
        IList,
        INotifyCollectionChanged,
        ICollectionChangedListener
    {
        /// <summary>
        /// Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public static ItemsSourceView Empty { get; } = new ItemsSourceView(Array.Empty<object?>());

        private IList _source;
        private NotifyCollectionChangedEventHandler? _collectionChanged;
        private NotifyCollectionChangedEventHandler? _preCollectionChanged;
        private NotifyCollectionChangedEventHandler? _postCollectionChanged;
        private bool _listening;

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        private protected ItemsSourceView(IEnumerable source) => SetSource(source);

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count => Source.Count;

        /// <summary>
        /// Gets the source collection.
        /// </summary>
        public IList Source => _source;

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public object? this[int index] => GetAt(index);

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        object? IList.this[int index]
        {
            get => GetAt(index);
            set => ThrowReadOnly();
        }

        /// <summary>
        /// Not implemented in Avalonia, preserved here for ItemsRepeater's usage.
        /// </summary>
        internal bool HasKeyIndexMapping => false;

        /// <summary>
        /// Occurs when the collection has changed to indicate the reason for the change and which items changed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                AddListenerIfNecessary();
                _collectionChanged += value;
            }

            remove
            {
                _collectionChanged -= value;
                RemoveListenerIfNecessary();
            }
        }

        /// <summary>
        /// Occurs when a collection has finished changing and all <see cref="CollectionChanged"/>
        /// event handlers have been notified.
        /// </summary>
        internal event NotifyCollectionChangedEventHandler? PreCollectionChanged
        {
            add
            {
                AddListenerIfNecessary();
                _preCollectionChanged += value;
            }

            remove
            {
                _preCollectionChanged -= value;
                RemoveListenerIfNecessary();
            }
        }

        /// <summary>
        /// Occurs when a collection has finished changing and all <see cref="CollectionChanged"/>
        /// event handlers have been notified.
        /// </summary>
        internal event NotifyCollectionChangedEventHandler? PostCollectionChanged
        {
            add
            {
                AddListenerIfNecessary();
                _postCollectionChanged += value;
            }

            remove
            {
                _postCollectionChanged -= value;
                RemoveListenerIfNecessary();
            }
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public object? GetAt(int index) => Source[index];
        public bool Contains(object? item) => Source.Contains(item);
        public int IndexOf(object? item) => Source.IndexOf(item);

        /// <summary>
        /// Gets or creates an <see cref="ItemsSourceView"/> for the specified enumerable.
        /// </summary>
        /// <param name="items">The enumerable.</param>
        /// <remarks>
        /// This method handles the following three cases:
        /// - If <paramref name="items"/> is null, returns <see cref="Empty"/>
        /// - If <paramref name="items"/> is an <see cref="ItemsSourceView"/> returns the existing
        ///   <see cref="ItemsSourceView"/>
        /// - Otherwise creates a new <see cref="ItemsSourceView"/>
        /// </remarks>
        public static ItemsSourceView GetOrCreate(IEnumerable? items)
        {
            return items switch
            {
                ItemsSourceView isv => isv,
                null => Empty,
                _ => new ItemsSourceView(items)
            };
        }

        /// <summary>
        /// Gets or creates an <see cref="ItemsSourceView{T}"/> for the specified enumerable.
        /// </summary>
        /// <param name="items">The enumerable.</param>
        /// <remarks>
        /// This method handles the following three cases:
        /// - If <paramref name="items"/> is null, returns <see cref="Empty"/>
        /// - If <paramref name="items"/> is an <see cref="ItemsSourceView"/> returns the existing
        ///   <see cref="ItemsSourceView"/>
        /// - Otherwise creates a new <see cref="ItemsSourceView"/>
        /// </remarks>
        public static ItemsSourceView<T> GetOrCreate<T>(IEnumerable? items)
        {
            return items switch
            {
                ItemsSourceView<T> isvt => isvt,
                ItemsSourceView isv => new ItemsSourceView<T>(isv.Source),
                null => ItemsSourceView<T>.Empty,
                _ => new ItemsSourceView<T>(items)
            };
        }

        /// <summary>
        /// Gets or creates an <see cref="ItemsSourceView{T}"/> for the specified enumerable.
        /// </summary>
        /// <param name="items">The enumerable.</param>
        /// <remarks>
        /// This method handles the following three cases:
        /// - If <paramref name="items"/> is null, returns <see cref="Empty"/>
        /// - If <paramref name="items"/> is an <see cref="ItemsSourceView"/> returns the existing
        ///   <see cref="ItemsSourceView"/>
        /// - Otherwise creates a new <see cref="ItemsSourceView"/>
        /// </remarks>
        public static ItemsSourceView<T> GetOrCreate<T>(IEnumerable<T>? items)
        {
            return items switch
            {
                ItemsSourceView<T> isv => isv,
                null => ItemsSourceView<T>.Empty,
                _ => new ItemsSourceView<T>(items)
            };
        }

        public IEnumerator<object?> GetEnumerator()
        {
            static IEnumerator<object> EnumerateItems(IList list)
            {
                foreach (var o in list)
                    yield return o;
            }

            var inner = Source;

            return inner switch
            {
                IEnumerable<object> e => e.GetEnumerator(),
                _ => EnumerateItems(inner),
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => Source.GetEnumerator();

        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            _preCollectionChanged?.Invoke(this, e);
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            _collectionChanged?.Invoke(this, e);
        }

        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            _postCollectionChanged?.Invoke(this, e);
        }

        int IList.Add(object? value) => ThrowReadOnly();
        void IList.Clear() => ThrowReadOnly();
        void IList.Insert(int index, object? value) => ThrowReadOnly();
        void IList.Remove(object? value) => ThrowReadOnly();
        void IList.RemoveAt(int index) => ThrowReadOnly();
        void ICollection.CopyTo(Array array, int index) => Source.CopyTo(array, index);

        /// <summary>
        /// Not implemented in Avalonia, preserved here for ItemsRepeater's usage.
        /// </summary>
        internal string KeyFromIndex(int index) => throw new NotImplementedException();

        private protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            _preCollectionChanged?.Invoke(this, e);
            _collectionChanged?.Invoke(this, e);
            _postCollectionChanged?.Invoke(this, e);
        }

        [MemberNotNull(nameof(_source))]
        private protected void SetSource(IEnumerable source)
        {
            if (_listening && _source is INotifyCollectionChanged inccOld)
                CollectionChangedEventManager.Instance.RemoveListener(inccOld, this);

            _source = source switch
            {
                ItemsSourceView isv => isv.Source,
                IList list => list,
                INotifyCollectionChanged => throw new ArgumentException(
                    "Collection implements INotifyCollectionChanged but not IList.",
                    nameof(source)),
                IEnumerable<object> iObj => new List<object>(iObj),
                null => throw new ArgumentNullException(nameof(source)),
                _ => new List<object>(source.Cast<object>())
            };

            if (_listening && _source is INotifyCollectionChanged inccNew)
                CollectionChangedEventManager.Instance.AddListener(inccNew, this);
        }

        private void AddListenerIfNecessary()
        {
            if (!_listening)
            {
                if (_source is INotifyCollectionChanged incc)
                    CollectionChangedEventManager.Instance.AddListener(incc, this);
                _listening = true;
            }
        }

        private void RemoveListenerIfNecessary()
        {
            if (_listening && _collectionChanged is null && _postCollectionChanged is null)
            {
                if (_source is INotifyCollectionChanged incc)
                    CollectionChangedEventManager.Instance.RemoveListener(incc, this);
                _listening = false;
            }
        }

        [DoesNotReturn]
        private static int ThrowReadOnly() => throw new NotSupportedException("Collection is read-only.");
    }

    public sealed class ItemsSourceView<T> : ItemsSourceView, IReadOnlyList<T>
    {
        /// <summary>
        ///  Gets an empty <see cref="ItemsSourceView"/>
        /// </summary>
        public new static ItemsSourceView<T> Empty { get; } = new ItemsSourceView<T>(Array.Empty<T>());

        /// <summary>
        /// Initializes a new instance of the ItemsSourceView class for the specified data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        internal ItemsSourceView(IEnumerable<T> source)
            : base(source)
        {
        }

        internal ItemsSourceView(IEnumerable source)
            : base(source)
        {
        }

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public new T this[int index] => GetAt(index);

        /// <summary>
        /// Retrieves the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item.</returns>
        public new T GetAt(int index) => (T)Source[index]!;

        public new IEnumerator<T> GetEnumerator()
        {
            static IEnumerator<T> EnumerateItems(IList list)
            {
                foreach (var o in list)
                    yield return (T)o;
            }

            var inner = Source;

            return inner switch
            {
                IEnumerable<T> e => e.GetEnumerator(),
                _ => EnumerateItems(inner),
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => Source.GetEnumerator();
    }
}
