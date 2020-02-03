using System;
using System.Collections.Generic;

#nullable enable

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
            var deselectedCount = 0;
            var selectedCount = 0;

            foreach (var change in _changes)
            {
                deselectedCount += change.DeselectedCount;
                selectedCount += change.SelectedCount;
            }

            var deselectedIndices = new SelectedItems<IndexPath, SelectionNodeOperation>(
                _changes,
                deselectedCount,
                GetDeselectedIndexAt);
            var selectedIndices = new SelectedItems<IndexPath, SelectionNodeOperation>(
                _changes,
                selectedCount,
                GetSelectedIndexAt);
            var deselectedItems = new SelectedItems<object?, SelectionNodeOperation>(
                _changes,
                deselectedCount,
                GetDeselectedItemAt);
            var selectedItems = new SelectedItems<object?, SelectionNodeOperation>(
                _changes,
                selectedCount,
                GetSelectedItemAt);

            return new SelectionModelSelectionChangedEventArgs(
                deselectedIndices,
                selectedIndices,
                deselectedItems,
                selectedItems);
        }

        private IndexPath GetDeselectedIndexAt(
            List<SelectionNodeOperation> infos,
            int index)
        {
            static int GetCount(SelectionNodeOperation info) => info.DeselectedCount;
            static List<IndexRange>? GetRanges(SelectionNodeOperation info) => info.DeselectedRanges;
            return GetIndexAt(infos, index, GetCount, GetRanges);
        }

        private IndexPath GetSelectedIndexAt(
            List<SelectionNodeOperation> infos,
            int index)
        {
            static int GetCount(SelectionNodeOperation info) => info.SelectedCount;
            static List<IndexRange>? GetRanges(SelectionNodeOperation info) => info.SelectedRanges;
            return GetIndexAt(infos, index, GetCount, GetRanges);
        }

        private object? GetDeselectedItemAt(
            List<SelectionNodeOperation> infos,
            int index)
        {
            static int GetCount(SelectionNodeOperation info) => info.DeselectedCount;
            static List<IndexRange>? GetRanges(SelectionNodeOperation info) => info.DeselectedRanges;
            return GetItemAt(infos, index, GetCount, GetRanges);
        }

        private object? GetSelectedItemAt(
            List<SelectionNodeOperation> infos,
            int index)
        {
            static int GetCount(SelectionNodeOperation info) => info.SelectedCount;
            static List<IndexRange>? GetRanges(SelectionNodeOperation info) => info.SelectedRanges;
            return GetItemAt(infos, index, GetCount, GetRanges);
        }

        private IndexPath GetIndexAt(
            List<SelectionNodeOperation> infos,
            int index,
            Func<SelectionNodeOperation, int> getCount,
            Func<SelectionNodeOperation, List<IndexRange>?> getRanges)
        {
            var currentIndex = 0;
            IndexPath path = default;

            foreach (var info in infos)
            {
                var currentCount = getCount(info);

                if (index >= currentIndex && index < currentIndex + currentCount)
                {
                    int targetIndex = GetIndexAt(getRanges(info), index - currentIndex);
                    path = info.Path.CloneWithChildIndex(targetIndex);
                    break;
                }

                currentIndex += currentCount;
            }

            return path;
        }

        private object? GetItemAt(
            List<SelectionNodeOperation> infos,
            int index,
            Func<SelectionNodeOperation, int> getCount,
            Func<SelectionNodeOperation, List<IndexRange>?> getRanges)
        {
            var currentIndex = 0;
            object? item = null;

            foreach (var info in infos)
            {
                var currentCount = getCount(info);

                if (index >= currentIndex && index < currentIndex + currentCount)
                {
                    int targetIndex = GetIndexAt(getRanges(info), index - currentIndex);
                    item = info.Items?.GetAt(targetIndex);
                    break;
                }

                currentIndex += currentCount;
            }

            return item;
        }

        private int GetIndexAt(List<IndexRange>? ranges, int index)
        {
            var currentIndex = 0;

            if (ranges != null)
            {
                foreach (var range in ranges)
                {
                    var currentCount = (range.End - range.Begin) + 1;

                    if (index >= currentIndex && index < currentIndex + currentCount)
                    {
                        return range.Begin + (index - currentIndex);
                    }

                    currentIndex += currentCount;
                }
            }

            throw new IndexOutOfRangeException();
        }
    }
}
