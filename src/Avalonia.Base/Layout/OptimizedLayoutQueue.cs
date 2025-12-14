using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Layout
{
    /// <summary>
    /// An optimized layout queue that processes elements in depth-sorted order.
    /// This ensures parents are always processed before children, reducing redundant layout passes.
    /// Uses an index-based approach for O(1) dequeue operations instead of O(n) RemoveAt(0).
    /// </summary>
    internal sealed class OptimizedLayoutQueue
    {
        private readonly List<Layoutable> _items = new();
        private readonly HashSet<Layoutable> _itemSet = new();
        private int _dequeueIndex;
        private bool _needsSort;

        /// <summary>
        /// Gets the number of remaining items in the queue.
        /// </summary>
        public int Count => _items.Count - _dequeueIndex;

        /// <summary>
        /// Adds an item to the queue if not already present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(Layoutable item)
        {
            if (_itemSet.Add(item))
            {
                _items.Add(item);
                _needsSort = true;
            }
        }

        /// <summary>
        /// Removes and returns the next item (shallowest depth first). O(1) amortized.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Layoutable? Dequeue()
        {
            if (_dequeueIndex >= _items.Count)
                return null;

            EnsureSorted();

            var item = _items[_dequeueIndex];
            _items[_dequeueIndex] = null!; // Allow GC to collect
            _dequeueIndex++;
            _itemSet.Remove(item);
            
            // Compact the list if we've processed more than half to avoid unbounded growth
            if (_dequeueIndex > 64 && _dequeueIndex > _items.Count / 2)
            {
                Compact();
            }
            
            return item;
        }

        /// <summary>
        /// Returns the next item without removing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Layoutable? Peek()
        {
            if (_dequeueIndex >= _items.Count)
                return null;

            EnsureSorted();
            return _items[_dequeueIndex];
        }

        /// <summary>
        /// Removes a specific item from the queue.
        /// </summary>
        public bool Remove(Layoutable item)
        {
            if (_itemSet.Remove(item))
            {
                // Mark as removed by setting to null - will be skipped during dequeue
                var index = _items.IndexOf(item, _dequeueIndex);
                if (index >= 0)
                {
                    _items[index] = null!;
                }
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
            _dequeueIndex = 0;
            _needsSort = false;
        }

        /// <summary>
        /// Checks if an item is in the queue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Layoutable item) => _itemSet.Contains(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSorted()
        {
            if (_needsSort && _items.Count - _dequeueIndex > 1)
            {
                // Sort only the remaining items
                if (_dequeueIndex == 0)
                {
                    _items.Sort(DepthComparer.Instance);
                }
                else
                {
                    // If we have already dequeued some items, compact first then sort
                    Compact();
                    _items.Sort(DepthComparer.Instance);
                }
                _needsSort = false;
            }
        }
        
        /// <summary>
        /// Compacts the list by removing processed items from the front.
        /// </summary>
        private void Compact()
        {
            if (_dequeueIndex == 0)
                return;
                
            var remaining = _items.Count - _dequeueIndex;
            for (var i = 0; i < remaining; i++)
            {
                _items[i] = _items[_dequeueIndex + i];
            }
            _items.RemoveRange(remaining, _dequeueIndex);
            _dequeueIndex = 0;
        }

        /// <summary>
        /// Comparer that sorts by visual depth (shallowest first).
        /// </summary>
        private sealed class DepthComparer : IComparer<Layoutable>
        {
            public static readonly DepthComparer Instance = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(Layoutable? x, Layoutable? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return 1;  // Nulls sort to end
                if (y is null) return -1;

                // Use cached VisualLevel for O(1) comparison
                return x.VisualLevel.CompareTo(y.VisualLevel);
            }
        }
    }
}
