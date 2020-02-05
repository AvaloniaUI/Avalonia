// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    public interface ISelectionModel
    {
        IndexPath AnchorIndex { get; set; }
        IndexPath SelectedIndex { get; set; }
        IReadOnlyList<IndexPath> SelectedIndices { get; }
        object SelectedItem { get; }
        IReadOnlyList<object> SelectedItems { get; }
        bool SingleSelect { get; set; }
        bool AutoSelect { get; set; }
        object Source { get; set; }

        event EventHandler<SelectionModelChildrenRequestedEventArgs> ChildrenRequested;
        event EventHandler<SelectionModelSelectionChangedEventArgs> SelectionChanged;

        void ClearSelection();
        void Deselect(int index);
        void Deselect(int groupIndex, int itemIndex);
        void DeselectAt(IndexPath index);
        void DeselectRange(IndexPath start, IndexPath end);
        void DeselectRangeFromAnchor(int index);
        void DeselectRangeFromAnchor(int endGroupIndex, int endItemIndex);
        void DeselectRangeFromAnchorTo(IndexPath index);
        void Dispose();
        bool? IsSelected(int index);
        bool? IsSelected(int groupIndex, int itemIndex);
        bool? IsSelectedAt(IndexPath index);
        void Select(int index);
        void Select(int groupIndex, int itemIndex);
        void SelectAll();
        void SelectAt(IndexPath index);
        void SelectRange(IndexPath start, IndexPath end);
        void SelectRangeFromAnchor(int index);
        void SelectRangeFromAnchor(int endGroupIndex, int endItemIndex);
        void SelectRangeFromAnchorTo(IndexPath index);
        void SetAnchorIndex(int index);
        void SetAnchorIndex(int groupIndex, int index);
    }
}
