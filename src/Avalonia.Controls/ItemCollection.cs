﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds the list of items that constitute the content of an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemCollection : ItemsSourceView, IList
    {
        private Mode _mode;

        internal ItemCollection()
            : base(UninitializedSource)
        {
        }

        public new object? this[int index]
        {
            get => base[index];
            set => WritableSource[index] = value;
        }

        public bool IsReadOnly => _mode == Mode.ItemsSource;

        internal event EventHandler? SourceChanged;

        /// <summary>
        /// Adds an item to the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="value">The item to add to the collection.</param>
        /// <returns>
        /// The position into which the new element was inserted, or -1 to indicate that
        /// the item was not inserted into the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The collection is in ItemsSource mode.
        /// </exception>
        public int Add(object? value) => WritableSource.Add(value);

        /// <summary>
        /// Clears the collection and releases the references on all items currently in the
        /// collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The collection is in ItemsSource mode.
        /// </exception>
        public void Clear() => WritableSource.Clear();

        /// <summary>
        /// Inserts an element into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="value">The item to insert.</param>
        /// <exception cref="InvalidOperationException">
        /// The collection is in ItemsSource mode.
        /// </exception>
        public void Insert(int index, object? value) => WritableSource.Insert(index, value);

        /// <summary>
        /// Removes the item at the specified index of the collection or view.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="InvalidOperationException">
        /// The collection is in ItemsSource mode.
        /// </exception>
        public void RemoveAt(int index) => WritableSource.RemoveAt(index);

        /// <summary>
        /// Removes the specified item reference from the collection or view.
        /// </summary>
        /// <param name="value">The object to remove.</param>
        /// <returns>True if the item was removed; otherwise false.</returns>
        /// <exception cref="InvalidOperationException">
        /// The collection is in ItemsSource mode.
        /// </exception>
        public bool Remove(object? value)
        {
            var c = Count;
            WritableSource.Remove(value);
            return Count < c;
        }

        int IList.Add(object? value) => Add(value);
        void IList.Clear() => Clear();
        void IList.Insert(int index, object? value) => Insert(index, value);
        void IList.RemoveAt(int index) => RemoveAt(index);

        private IList WritableSource
        {
            get
            {
                if (IsReadOnly)
                    ThrowIsItemsSource();
                if (Source == UninitializedSource)
                    SetSource(CreateDefaultCollection());
                return Source;
            }
        }

        internal void SetItemsSource(IEnumerable? value)
        {
            if (_mode != Mode.ItemsSource && Count > 0)
                throw new InvalidOperationException(
                    "Items collection must be empty before using ItemsSource.");

            _mode = value is not null ? Mode.ItemsSource : Mode.Items;
            SetSource(value ?? CreateDefaultCollection());
        }

        private new void SetSource(IEnumerable source)
        {
            var oldSource = Source;

            base.SetSource(source);

            if (oldSource.Count > 0)
                RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, oldSource, 0));
            if (Source.Count > 0)
                RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, Source, 0));
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }

        private static AvaloniaList<object?> CreateDefaultCollection()
        {
            return new() { ResetBehavior = ResetBehavior.Remove };
        }

        [DoesNotReturn]
        private static void ThrowIsItemsSource()
        {
            throw new InvalidOperationException(
                "Operation is not valid while ItemsSource is in use." +
                "Access and modify elements with ItemsControl.ItemsSource instead.");
        }

        private enum Mode
        {
            Items,
            ItemsSource,
        }
    }
}
