using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls
{
    internal class SelectionNodeOperation
    {
        private readonly SelectionNode _owner;
        private List<IndexRange>? _selected;
        private List<IndexRange>? _deselected;

        public SelectionNodeOperation(SelectionNode owner)
        {
            _owner = owner;
        }

        public bool HasChanges => _selected?.Count > 0 || _deselected?.Count > 0;
        public List<IndexRange>? SelectedRanges => _selected;
        public List<IndexRange>? DeselectedRanges => _deselected;
        public IndexPath Path => _owner.IndexPath;
        public ItemsSourceView? Items => _owner.ItemsSourceView;

        public void Selected(IndexRange range)
        {
            Add(range, ref _selected, _deselected);
        }

        public void Selected(IEnumerable<IndexRange> ranges)
        {
            foreach (var range in ranges)
            {
                Selected(range);
            }
        }

        public void Deselected(IndexRange range)
        {
            Add(range, ref _deselected, _selected);
        }

        public void Deselected(IEnumerable<IndexRange> ranges)
        {
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
    }
}
