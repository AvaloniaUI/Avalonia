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
        public SelectionModelSelectionChangedEventArgs(
            IReadOnlyList<IndexPath>? deselectedIndices,
            IReadOnlyList<IndexPath>? selectedIndices,
            IReadOnlyList<object>? deselectedItems,
            IReadOnlyList<object>? selectedItems)
        {
            DeselectedIndices = deselectedIndices ?? Array.Empty<IndexPath>();
            SelectedIndices = selectedIndices ?? Array.Empty<IndexPath>();
            DeselectedItems = deselectedItems ?? Array.Empty<object>();
            SelectedItems= selectedItems ?? Array.Empty<object>();
        }

        /// <summary>
        /// Gets the indices of the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<IndexPath> DeselectedIndices { get; }

        /// <summary>
        /// Gets the indices of the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<IndexPath> SelectedIndices { get; }

        /// <summary>
        /// Gets the items that were removed from the selection.
        /// </summary>
        public IReadOnlyList<object> DeselectedItems { get; }

        /// <summary>
        /// Gets the items that were added to the selection.
        /// </summary>
        public IReadOnlyList<object> SelectedItems { get; }
    }
}
