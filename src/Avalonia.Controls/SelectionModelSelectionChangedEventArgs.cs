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
        private readonly IEnumerable<IndexPath> _selectedIndicesSource;
        private readonly IEnumerable<IndexPath> _deselectedIndicesSource;
        private readonly IEnumerable<object> _selectedItemsSource;
        private readonly IEnumerable<object> _deselectedItemsSource;
        private List<IndexPath>? _selectedIndices;
        private List<IndexPath>? _deselectedIndices;
        private List<object>? _selectedItems;
        private List<object>? _deselectedItems;

        public SelectionModelSelectionChangedEventArgs(
            IEnumerable<IndexPath> deselectedIndices,
            IEnumerable<IndexPath> selectedIndices,
            IEnumerable<object> deselectedItems,
            IEnumerable<object> selectedItems)
        {
            _selectedIndicesSource = selectedIndices;
            _deselectedIndicesSource = deselectedIndices;
            _selectedItemsSource = selectedItems;
            _deselectedItemsSource = deselectedItems;
        }

        /// <summary>
        /// Gets the indices of the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<IndexPath> SelectedIndices =>
            _selectedIndices ?? (_selectedIndices = new List<IndexPath>(_selectedIndicesSource));

        /// <summary>
        /// Gets the indices of the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<IndexPath> DeselectedIndices =>
            _deselectedIndices ?? (_deselectedIndices = new List<IndexPath>(_deselectedIndicesSource));

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<object> SelectedItems =>
            _selectedItems ?? (_selectedItems = new List<object>(_selectedItemsSource));

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<object> DeselectedItems =>
            _deselectedItems ?? (_deselectedItems = new List<object>(_deselectedItemsSource));
    }
}
