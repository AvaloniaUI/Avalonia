using System;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    internal class SelectionModelChangeSet
    {
        private List<SelectionNodeOperation> _changes;

        public SelectionModelChangeSet(List<SelectionNodeOperation> changes)
        {
            _changes = changes;
        }

        public SelectionModelSelectionChangedEventArgs CreateEventArgs()
        {
            return new SelectionModelSelectionChangedEventArgs(
                CreateIndices(x => x.DeselectedRanges),
                CreateIndices(x => x.SelectedRanges),
                CreateItems(x => x.DeselectedRanges),
                CreateItems(x => x.SelectedRanges));
        }

        private IEnumerable<IndexPath> CreateIndices(Func<SelectionNodeOperation, List<IndexRange>?> selector)
        {
            if (_changes == null)
            {
                yield break;
            }

            foreach (var i in _changes)
            {
                var ranges = selector(i);

                if (ranges != null)
                {
                    foreach (var j in ranges)
                    {
                        for (var k = j.Begin; k <= j.End; ++k)
                        {
                            yield return i.Path.CloneWithChildIndex(k);
                        }
                    }
                }
            }
        }

        private IEnumerable<object> CreateItems(Func<SelectionNodeOperation, List<IndexRange>?> selector)
        {
            if (_changes == null)
            {
                yield break;
            }

            var result = new List<object>();

            foreach (var i in _changes)
            {
                var ranges = selector(i);

                if (ranges != null && i.Items != null)
                {
                    foreach (var j in ranges)
                    {
                        for (var k = j.Begin; k <= j.End; ++k)
                        {
                            yield return i.Items.GetAt(k);
                        }
                    }
                }
            }
        }
    }
}
