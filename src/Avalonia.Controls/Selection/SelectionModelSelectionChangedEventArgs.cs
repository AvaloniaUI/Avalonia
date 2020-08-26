using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls.Selection;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public abstract class SelectionModelSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the indexes of the items that were removed from the selection.
        /// </summary>
        public abstract IReadOnlyList<int> DeselectedIndexes { get; }

        /// <summary>
        /// Gets the indexes of the items that were added to the selection.
        /// </summary>
        public abstract IReadOnlyList<int> SelectedIndexes { get; }

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<object?> DeselectedItems => GetUntypedDeselectedItems();

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<object?> SelectedItems => GetUntypedSelectedItems();

        protected abstract IReadOnlyList<object?> GetUntypedDeselectedItems();
        protected abstract IReadOnlyList<object?> GetUntypedSelectedItems();
    }

    public class SelectionModelSelectionChangedEventArgs<T> : SelectionModelSelectionChangedEventArgs
    {
        private IReadOnlyList<object?>? _deselectedItems;
        private IReadOnlyList<object?>? _selectedItems;

        public SelectionModelSelectionChangedEventArgs(
            IReadOnlyList<int>? deselectedIndices = null,
            IReadOnlyList<int>? selectedIndices = null,
            IReadOnlyList<T>? deselectedItems = null,
            IReadOnlyList<T>? selectedItems = null)
        {
            DeselectedIndexes = deselectedIndices ?? Array.Empty<int>();
            SelectedIndexes = selectedIndices ?? Array.Empty<int>();
            DeselectedItems = deselectedItems ?? Array.Empty<T>();
            SelectedItems = selectedItems ?? Array.Empty<T>();
        }

        /// <summary>
        /// Gets the indexes of the items that were removed from the selection.
        /// </summary>
        public override IReadOnlyList<int> DeselectedIndexes { get; }

        /// <summary>
        /// Gets the indexes of the items that were added to the selection.
        /// </summary>
        public override IReadOnlyList<int> SelectedIndexes { get; }

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public new IReadOnlyList<T> DeselectedItems { get; }

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public new IReadOnlyList<T> SelectedItems { get; }

        protected override IReadOnlyList<object?> GetUntypedDeselectedItems()
        {
            return _deselectedItems ??= (DeselectedItems as IReadOnlyList<object?>) ??
                new SelectedItems<T>.Untyped(DeselectedItems);
        }

        protected override IReadOnlyList<object?> GetUntypedSelectedItems()
        {
            return _selectedItems ??= (SelectedItems as IReadOnlyList<object?>) ??
                new SelectedItems<T>.Untyped(SelectedItems);
        }
    }
}
