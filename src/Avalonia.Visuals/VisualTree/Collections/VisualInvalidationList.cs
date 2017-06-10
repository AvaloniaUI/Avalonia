using System;
using System.Collections.Generic;

namespace Avalonia.VisualTree.Collections
{
    /// <summary>
    /// Stores a list of dirty visuals ordered by their distance to the root visual.
    /// </summary>
    public class VisualInvalidationList
    {
        private List<IVisual> _inner = new List<IVisual>();

        /// <summary>
        /// Gets the number of dirty visuals.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Adds a visual to the dirty list.
        /// </summary>
        /// <param name="visual">The dirty visual.</param>
        public void Add(IVisual visual)
        {
            var (found, i) = Search(visual);

            if (!found)
            {
                _inner.Insert(i >= 0 ? i : ~i, visual);
            }
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            _inner.Clear();
        }

        /// <summary>
        /// Removes a visual from the dirty list.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>True if the visual was present in the list; otherwise false.</returns>
        public bool Remove(IVisual visual)
        {
            var (found, i) = Search(visual);

            if (found)
            {
                _inner.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to dequeue the visual with the lowest distance to root from the list.
        /// </summary>
        /// <returns>An <see cref="IVisual"/> or null if the collection is empty.</returns>
        public IVisual TryDequeue()
        {
            if (_inner.Count > 0)
            {
                var item = _inner[_inner.Count - 1];
                _inner.RemoveAt(_inner.Count - 1);
                return item;
            }

            return null;
        }

        private int BinarySearch(int distance)
        {
            var l = 0;
            var r = _inner.Count - 1;

            while (l <= r)
            {
                var m = l + (r - l >> 1);
                var i = _inner[m];

                if (i.DistanceFromRoot == distance)
                {
                    return m;
                }
                if (i.DistanceFromRoot > distance)
                {
                    l = m + 1;
                }
                else if (i.DistanceFromRoot < distance)
                {
                    r = m - 1;
                }                
            }

            return ~l;
        }

        private (bool found, int index) Search(IVisual visual)
        {
            var d = visual.DistanceFromRoot;
            var i = BinarySearch(visual.DistanceFromRoot);

            if (i >= 0)
            {
                while (i > 0 && _inner[i - 1].DistanceFromRoot == d)
                {
                    --i;
                }

                for (; i < _inner.Count; ++i)
                {
                    var item = _inner[i];

                    if (item == visual)
                    {
                        return (true, i);
                    }
                    else if (item.DistanceFromRoot != d)
                    {
                        break;
                    }
                }
            }

            return (false, i);
        }

        private class DistanceComparer : IComparer<IVisual>
        {
            public static readonly DistanceComparer Instance = new DistanceComparer();
            public int Compare(IVisual x, IVisual y) => y.DistanceFromRoot - x.DistanceFromRoot;
        }
    }
}