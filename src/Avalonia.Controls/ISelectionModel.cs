// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.Controls
{
    public interface ISelectionModel : INotifyPropertyChanged
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
        void Deselect(IndexPath index);
        void DeselectRange(IndexPath start, IndexPath end);
        void DeselectRangeFromAnchor(int index);
        void DeselectRangeFromAnchor(IndexPath index);
        void Dispose();
        bool IsSelected(int index);
        bool IsSelected(IndexPath index);
        bool? IsTreeSelected(IndexPath index);
        void Select(int index);
        void Select(IndexPath index);
        void SelectAll();
        void SelectRange(IndexPath start, IndexPath end);
        void SelectRangeFromAnchor(int index);
        void SelectRangeFromAnchor(IndexPath index);
        IDisposable Update();
    }
}
