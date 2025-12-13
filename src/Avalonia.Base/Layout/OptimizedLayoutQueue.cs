using System;
using System.Collections.Generic;

namespace Avalonia.Layout
{
    /// <summary>
    /// An optimized layout queue that processes elements in depth-sorted order.
    /// This ensures parents are always processed before children, reducing redundant layout passes.
    /// </summary>
    internal sealed class OptimizedLayoutQueue
    {
        private readonly List<Layoutable> _items = new();
        private readonly HashSet<Layoutable> _itemSet = new();
        private bool _needsSort;

        /// <summary>
        /// Gets the number of items in the queue.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Adds an item to the queue if not already present.
        /// </summary>
        public void Enqueue(Layoutable item)
        {
            if (_itemSet.Add(item))
            {
                _items.Add(item);
                _needsSort = true;
            }
        }

        /// <summary>
        /// Removes and returns the next item (shallowest depth first).
        /// </summary>
        public Layoutable? Dequeue()
        {
            if (_items.Count == 0)
                return null;

            EnsureSorted();

            var item = _items[0];
            _items.RemoveAt(0);
            _itemSet.Remove(item);
            return item;
        }

        /// <summary>
        /// Returns the next item without removing it.
        /// </summary>
        public Layoutable? Peek()
        {
            if (_items.Count == 0)
                return null;

            EnsureSorted();
            return _items[0];
        }

        /// <summary>
        /// Removes a specific item from the queue.
        /// </summary>
        public bool Remove(Layoutable item)
        {
            if (_itemSet.Remove(item))
            {
                _items.Remove(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all items from the queue.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _itemSet.Clear();
            _needsSort = false;
        }

        /// <summary>
        /// Checks if an item is in the queue.
        /// </summary>
        public bool Contains(Layoutable item) => _itemSet.Contains(item);

        private void EnsureSorted()
        {
            if (_needsSort && _items.Count > 1)
            {
                _items.Sort(DepthComparer.Instance);
                _needsSort = false;
            }
        }

        /// <summary>
        /// Comparer that sorts by visual depth (shallowest first).
        /// </summary>
        private sealed class DepthComparer : IComparer<Layoutable>
        {
            public static readonly DepthComparer Instance = new();

            public int Compare(Layoutable? x, Layoutable? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                // Use cached VisualLevel for O(1) comparison
                return x.VisualLevel.CompareTo(y.VisualLevel);
            }
        }
    }
}
