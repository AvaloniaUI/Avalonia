// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds the selected items for a control.
    /// </summary>
    public interface ISelectionModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the anchor index.
        /// </summary>
        IndexPath AnchorIndex { get; set; }

        /// <summary>
        /// Gets or set the index of the first selected item.
        /// </summary>
        IndexPath SelectedIndex { get; set; }

        /// <summary>
        /// Gets or set the indexes of the selected items.
        /// </summary>
        IReadOnlyList<IndexPath> SelectedIndices { get; }

        /// <summary>
        /// Gets the first selected item.
        /// </summary>
        object SelectedItem { get; }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        IReadOnlyList<object> SelectedItems { get; }
        
        /// <summary>
        /// Gets a value indicating whether the model represents a single or multiple selection.
        /// </summary>
        bool SingleSelect { get; set; }

        /// <summary>
        /// Gets a value indicating whether to always keep an item selected where possible.
        /// </summary>
        bool AutoSelect { get; set; }

        /// <summary>
        /// Gets or sets the collection that contains the items that can be selected.
        /// </summary>
        object Source { get; set; }

        /// <summary>
        /// Raised when the children of a selection are required.
        /// </summary>
        event EventHandler<SelectionModelChildrenRequestedEventArgs> ChildrenRequested;

        /// <summary>
        /// Raised when the selection has changed.
        /// </summary>
        event EventHandler<SelectionModelSelectionChangedEventArgs> SelectionChanged;

        /// <summary>
        /// Clears the selection.
        /// </summary>
        void ClearSelection();

        /// <summary>
        /// Deselects an item.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        void Deselect(int index);

        /// <summary>
        /// Deselects an item.
        /// </summary>
        /// <param name="groupIndex">The index of the item group.</param>
        /// <param name="itemIndex">The index of the item in the group.</param>
        void Deselect(int groupIndex, int itemIndex);

        /// <summary>
        /// Deselects an item.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        void DeselectAt(IndexPath index);

        /// <summary>
        /// Deselects a range of items.
        /// </summary>
        /// <param name="start">The start index of the range.</param>
        /// <param name="end">The end index of the range.</param>
        void DeselectRange(IndexPath start, IndexPath end);

        /// <summary>
        /// Deselects a range of items, starting at <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="index">The end index of the range.</param>
        void DeselectRangeFromAnchor(int index);

        /// <summary>
        /// Deselects a range of items, starting at <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="endGroupIndex">
        /// The index of the item group that represents the end of the selection.
        /// </param>
        /// <param name="endItemIndex">
        /// The index of the item in the group that represents the end of the selection.
        /// </param>
        void DeselectRangeFromAnchor(int endGroupIndex, int endItemIndex);

        /// <summary>
        /// Deselects a range of items, starting at <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="index">The end index of the range.</param>
        void DeselectRangeFromAnchorTo(IndexPath index);

        /// <summary>
        /// Disposes the object and clears the selection.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Checks whether an item is selected.
        /// </summary>
        /// <param name="index">The index of the item</param>
        bool IsSelected(int index);

        /// <summary>
        /// Checks whether an item is selected.
        /// </summary>
        /// <param name="groupIndex">The index of the item group.</param>
        /// <param name="itemIndex">The index of the item in the group.</param>
        bool IsSelected(int groupIndex, int itemIndex);

        /// <summary>
        /// Checks whether an item is selected.
        /// </summary>
        /// <param name="index">The index of the item</param>
        public bool IsSelectedAt(IndexPath index);

        /// <summary>
        /// Checks whether an item or its descendents are selected.
        /// </summary>
        /// <param name="index">The index of the item</param>
        /// <returns>
        /// True if the item and all its descendents are selected, false if the item and all its
        /// descendents are deselected, or null if a combination of selected and deselected.
        /// </returns>
        bool? IsSelectedWithPartial(int index);

        /// <summary>
        /// Checks whether an item or its descendents are selected.
        /// </summary>
        /// <param name="groupIndex">The index of the item group.</param>
        /// <param name="itemIndex">The index of the item in the group.</param>
        /// <returns>
        /// True if the item and all its descendents are selected, false if the item and all its
        /// descendents are deselected, or null if a combination of selected and deselected.
        /// </returns>
        bool? IsSelectedWithPartial(int groupIndex, int itemIndex);

        /// <summary>
        /// Checks whether an item or its descendents are selected.
        /// </summary>
        /// <param name="index">The index of the item</param>
        /// <returns>
        /// True if the item and all its descendents are selected, false if the item and all its
        /// descendents are deselected, or null if a combination of selected and deselected.
        /// </returns>
        bool? IsSelectedWithPartialAt(IndexPath index);

        /// <summary>
        /// Selects an item.
        /// </summary>
        /// <param name="index">The index of the item</param>
        void Select(int index);

        /// <summary>
        /// Selects an item.
        /// </summary>
        /// <param name="groupIndex">The index of the item group.</param>
        /// <param name="itemIndex">The index of the item in the group.</param>
        void Select(int groupIndex, int itemIndex);

        /// <summary>
        /// Selects an item.
        /// </summary>
        /// <param name="index">The index of the item</param>
        void SelectAt(IndexPath index);

        /// <summary>
        /// Selects all items.
        /// </summary>
        void SelectAll();

        /// <summary>
        /// Selects a range of items.
        /// </summary>
        /// <param name="start">The start index of the range.</param>
        /// <param name="end">The end index of the range.</param>
        void SelectRange(IndexPath start, IndexPath end);

        /// <summary>
        /// Selects a range of items, starting at <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="index">The end index of the range.</param>
        void SelectRangeFromAnchor(int index);

        /// <summary>
        /// Selects a range of items, starting at <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="endGroupIndex">
        /// The index of the item group that represents the end of the selection.
        /// </param>
        /// <param name="endItemIndex">
        /// The index of the item in the group that represents the end of the selection.
        /// </param>
        void SelectRangeFromAnchor(int endGroupIndex, int endItemIndex);

        /// <summary>
        /// Selects a range of items, starting at <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="index">The end index of the range.</param>
        void SelectRangeFromAnchorTo(IndexPath index);

        /// <summary>
        /// Sets the <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="index">The anchor index.</param>
        void SetAnchorIndex(int index);

        /// <summary>
        /// Sets the <see cref="AnchorIndex"/>.
        /// </summary>
        /// <param name="groupIndex">The index of the item group.</param>
        /// <param name="index">The index of the item in the group.</param>
        void SetAnchorIndex(int groupIndex, int index);

        /// <summary>
        /// Begins a batch update of the selection.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that finishes the batch update.</returns>
        IDisposable Update();
    }
}
