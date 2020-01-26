using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls
{
    internal class SelectionModelChangeSet
    {
        private SelectionNode _owner;
        private List<IndexRange>? _selected;
        private List<IndexRange>? _deselected;

        public SelectionModelChangeSet(SelectionNode owner) => _owner = owner;

        public bool IsTracking { get; private set; }
        public bool HasChanges => _selected?.Count > 0 || _deselected?.Count > 0;
        public IEnumerable<IndexPath> SelectedIndices => EnumerateIndices(_selected);
        public IEnumerable<IndexPath> DeselectedIndices => EnumerateIndices(_deselected);
        public IEnumerable<object> SelectedItems => EnumerateItems(_selected);
        public IEnumerable<object> DeselectedItems => EnumerateItems(_deselected);

        public void BeginOperation()
        {
            if (IsTracking)
            {
                throw new AvaloniaInternalException("SelectionModel change operation already in progress.");
            }

            IsTracking = true;
            _selected?.Clear();
            _deselected?.Clear();
        }

        public void EndOperation() => IsTracking = false;

        public void Selected(IndexRange range)
        {
            if (!IsTracking)
            {
                return;
            }

            Add(range, ref _selected, _deselected);
        }

        public void Selected(IEnumerable<IndexRange> ranges)
        {
            if (!IsTracking)
            {
                return;
            }

            foreach (var range in ranges)
            {
                Selected(range);
            }
        }

        public void Deselected(IndexRange range)
        {
            if (!IsTracking)
            {
                return;
            }

            Add(range, ref _deselected, _selected);
        }

        public void Deselected(IEnumerable<IndexRange> ranges)
        {
            if (!IsTracking)
            {
                return;
            }

            foreach (var range in ranges)
            {
                Deselected(range);
            }
        }

        private static void Add(
            IndexRange range,
            ref List<IndexRange>? add,
            List<IndexRange>? remove)
        {
            if (remove != null)
            {
                var removed = new List<IndexRange>();
                IndexRange.Remove(remove, range, removed);
                var selected = IndexRange.Subtract(range, removed);

                if (selected.Any())
                {
                    add ??= new List<IndexRange>();

                    foreach (var r in selected)
                    {
                        IndexRange.Add(add, r);
                    }
                }
            }
            else
            {
                add ??= new List<IndexRange>();
                IndexRange.Add(add, range);
            }
        }

        private IEnumerable<IndexPath> EnumerateIndices(IEnumerable<IndexRange>? ranges)
        {
            var path = _owner.IndexPath;

            if (ranges != null)
            {
                foreach (var range in ranges)
                {
                    for (var i = range.Begin; i <= range.End; ++i)
                    {
                        yield return path.CloneWithChildIndex(i);
                    }
                }
            }
        }

        private IEnumerable<object> EnumerateItems(IEnumerable<IndexRange>? ranges)
        {
            var items = _owner.ItemsSourceView;

            if (ranges != null && items != null)
            {
                foreach (var range in ranges)
                {
                    for (var i = range.Begin; i <= range.End; ++i)
                    {
                        yield return items.GetAt(i);
                    }
                }
            }
        }
    }
}
