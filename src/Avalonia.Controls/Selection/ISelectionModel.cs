using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public interface ISelectionModel : INotifyPropertyChanged
    {
        IEnumerable? Source { get; set; }
        bool SingleSelect { get; set; }
        int SelectedIndex { get; set; }
        IReadOnlyList<int> SelectedIndexes { get; }
        object? SelectedItem { get; set; }
        IReadOnlyList<object?> SelectedItems { get; }
        int AnchorIndex { get; set; }
        int Count { get; }

        public event EventHandler<SelectionModelIndexesChangedEventArgs>? IndexesChanged;
        public event EventHandler<SelectionModelSelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler? LostSelection;
        public event EventHandler? SourceReset;

        public void BeginBatchUpdate();
        public void EndBatchUpdate();
        bool IsSelected(int index);
        void Select(int index);
        void Deselect(int index);
        void SelectRange(int start, int end);
        void DeselectRange(int start, int end);
        void SelectAll();
        void Clear();
    }

    public static class SelectionModelExtensions
    {
        public static IDisposable BatchUpdate(this ISelectionModel model)
        {
            return new BatchUpdateOperation(model);
        }

        public struct BatchUpdateOperation : IDisposable
        {
            private readonly ISelectionModel _owner;
            private bool _isDisposed;

            public BatchUpdateOperation(ISelectionModel owner)
            {
                _owner = owner;
                _isDisposed = false;
                owner.BeginBatchUpdate();
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _owner?.EndBatchUpdate();
                    _isDisposed = true;
                }
            }
        }
    }
}
