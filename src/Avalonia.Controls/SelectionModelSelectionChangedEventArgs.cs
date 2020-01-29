// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls
{
    public class SelectionModelSelectionChangedEventArgs : EventArgs
    {
        private readonly IEnumerable<IndexPath>? _deselectedIndicesSource;
        private readonly IEnumerable<IndexPath>? _selectedIndicesSource;
        private readonly IEnumerable<object>? _deselectedItemsSource;
        private readonly IEnumerable<object>? _selectedItemsSource;
        private IReadOnlyList<IndexPath>? _deselectedIndices;
        private IReadOnlyList<IndexPath>? _selectedIndices;
        private IReadOnlyList<object>? _deselectedItems;
        private IReadOnlyList<object>? _selectedItems;

        public SelectionModelSelectionChangedEventArgs(
            IReadOnlyList<IndexPath>? deselectedIndices,
            IReadOnlyList<IndexPath>? selectedIndices,
            IReadOnlyList<object>? deselectedItems,
            IReadOnlyList<object>? selectedItems)
        {
            _deselectedIndices = deselectedIndices ?? Array.Empty<IndexPath>();
            _selectedIndices = selectedIndices ?? Array.Empty<IndexPath>();
            _deselectedItems = deselectedItems ?? Array.Empty<object>();
            _selectedItems= selectedItems ?? Array.Empty<object>();
        }

        public SelectionModelSelectionChangedEventArgs(
            IEnumerable<IndexPath>? deselectedIndices,
            IEnumerable<IndexPath>? selectedIndices,
            IEnumerable<object>? deselectedItems,
            IEnumerable<object>? selectedItems)
        {
            static void Set<T>(IEnumerable<T>? source, ref IEnumerable<T>? sourceField, ref IReadOnlyList<T>? field)
            {
                if (source != null)
                {
                    sourceField = source;
                }
                else
                {
                    field = Array.Empty<T>();
                }
            }

            Set(deselectedIndices, ref _deselectedIndicesSource, ref _deselectedIndices);
            Set(selectedIndices, ref _selectedIndicesSource, ref _selectedIndices);
            Set(deselectedItems, ref _deselectedItemsSource, ref _deselectedItems);
            Set(selectedItems, ref _selectedItemsSource, ref _selectedItems);
        }

        /// <summary>
        /// Gets the indices of the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<IndexPath> DeselectedIndices
            => _deselectedIndices ??= new List<IndexPath>(_deselectedIndicesSource);

        /// <summary>
        /// Gets the indices of the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<IndexPath> SelectedIndices
            => _selectedIndices ??= new List<IndexPath>(_selectedIndicesSource);

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<object> DeselectedItems
            => _deselectedItems ??= new List<object>(_deselectedItemsSource);

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<object> SelectedItems
            => _selectedItems ??= new List<object>(_selectedItemsSource);
    }
}
