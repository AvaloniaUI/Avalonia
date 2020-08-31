using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class SelectedIndexes<T> : IReadOnlyList<int>
    {
        private readonly SelectionModel<T>? _owner;
        private readonly IReadOnlyList<IndexRange>? _ranges;

        public SelectedIndexes(SelectionModel<T> owner) => _owner = owner;
        public SelectedIndexes(IReadOnlyList<IndexRange> ranges) => _ranges = ranges;

        public int this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException("The index was out of range.");
                }

                if (_owner?.SingleSelect == true)
                {
                    return _owner.SelectedIndex;
                }
                else
                {
                    return IndexRange.GetAt(Ranges!, index);
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
                    return IndexRange.GetCount(Ranges!);
                }
            }
        }

        private IReadOnlyList<IndexRange> Ranges => _ranges ?? _owner!.Ranges!;

        public IEnumerator<int> GetEnumerator()
        {
            IEnumerator<int> SingleSelect()
            {
                if (_owner.SelectedIndex >= 0)
                {
                    yield return _owner.SelectedIndex;
                }
            }

            if (_owner?.SingleSelect == true)
            {
                return SingleSelect();
            }
            else
            {
                return IndexRange.EnumerateIndices(Ranges).GetEnumerator();
            }
        }

        public static SelectedIndexes<T>? Create(IReadOnlyList<IndexRange>? ranges)
        {
            return ranges is object ? new SelectedIndexes<T>(ranges) : null;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
