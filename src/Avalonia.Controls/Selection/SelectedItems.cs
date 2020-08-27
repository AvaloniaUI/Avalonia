using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class SelectedItems<T> : IReadOnlyList<T>
    {
        private readonly SelectionModel<T>? _owner;
        private readonly ItemsSourceView<T>? _items;
        private readonly IReadOnlyList<IndexRange>? _ranges;

        public SelectedItems(SelectionModel<T> owner) => _owner = owner;
        
        public SelectedItems(IReadOnlyList<IndexRange> ranges, ItemsSourceView<T>? items)
        {
            _ranges = ranges ?? throw new ArgumentNullException(nameof(ranges));
            _items = items;
        }

        [MaybeNull]
        public T this[int index]
        {
#pragma warning disable CS8766
            get
#pragma warning restore CS8766
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException("The index was out of range.");
                }

                if (_owner?.SingleSelect == true)
                {
                    return _owner.SelectedItem;
                }
                else if (Items is object)
                {
                    return Items[index];
                }
                else
                {
                    return default;
                }
            }
        }

        public int Count
        {
            get
            {
                if (_owner?.SingleSelect == true)
                {
                    return _owner.SelectedIndex == -1 ? 0 : 1;
                }
                else
                {
                    return Ranges is object ? IndexRange.GetCount(Ranges) : 0;
                }
            }
        }

        private ItemsSourceView<T>? Items => _items ?? _owner?.ItemsView;
        private IReadOnlyList<IndexRange>? Ranges => _ranges ?? _owner!.Ranges;

        public IEnumerator<T> GetEnumerator()
        {
            if (_owner?.SingleSelect == true)
            {
                if (_owner.SelectedIndex >= 0)
                {
#pragma warning disable CS8603
                    yield return _owner.SelectedItem;
#pragma warning restore CS8603
                }
            }
            else
            {
                var items = Items;

                foreach (var range in Ranges!)
                {
                    for (var i = range.Begin; i <= range.End; ++i)
                    {
#pragma warning disable CS8603
                        yield return items is object ? items[i] : default;
#pragma warning restore CS8603
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static SelectedItems<T>? Create(
            IReadOnlyList<IndexRange>? ranges,
            ItemsSourceView<T>? items)
        {
            return ranges is object ? new SelectedItems<T>(ranges, items) : null;
        }

        public class Untyped : IReadOnlyList<object?>
        {
            private readonly IReadOnlyList<T> _source;
            public Untyped(IReadOnlyList<T> source) => _source = source;
            public object? this[int index] => _source[index];
            public int Count => _source.Count;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<object?> GetEnumerator()
            {
                foreach (var i in _source)
                {
                    yield return i;
                }
            }
        }
    }
}
