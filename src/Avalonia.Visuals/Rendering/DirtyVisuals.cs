using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Stores a list of dirty visuals ordered by their distance to the root visual.
    /// </summary>
    /// <remarks>
    /// TODO:
    /// - This collection probably sucks performance and memory-wise, it needs rewriting using a
    ///   better data structure.
    /// - We probably want to put an upper limit on the number of visuals that can be
    ///   stored and if we reach that limit, assume all visuals are dirty.
    /// - Give it a better name and keep it somewhere else.
    /// </remarks>
    public class DirtyVisuals : IEnumerable<IVisual>
    {
        private SortedDictionary<int, List<IVisual>> _inner = new SortedDictionary<int, List<IVisual>>();
        private Dictionary<IVisual, int> _index = new Dictionary<IVisual, int>();

        /// <summary>
        /// Gets the number of dirty visuals.
        /// </summary>
        public int Count => _index.Count;

        /// <summary>
        /// Adds a visual to the dirty list.
        /// </summary>
        /// <param name="visual">The dirty visual.</param>
        public void Add(IVisual visual)
        {
            var distance = visual.CalculateDistanceFromAncestor(visual.VisualRoot);

            if (_index.TryGetValue(visual, out int existingDistance))
            {
                if (distance == existingDistance)
                {
                    return;
                }

                _inner[existingDistance].Remove(visual);
                _index.Remove(visual);
            }


            if (!_inner.TryGetValue(distance, out List<IVisual> list))
            {
                list = new List<IVisual>();
                _inner.Add(distance, list);
            }

            list.Add(visual);
            _index.Add(visual, distance);
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            _inner.Clear();
            _index.Clear();
        }

        /// <summary>
        /// Removes a visual from the dirty list.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>True if the visual was present in the list; otherwise false.</returns>
        public bool Remove(IVisual visual)
        {
            if (_index.TryGetValue(visual, out int distance))
            {
                _inner[distance].Remove(visual);
                _index.Remove(visual);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the dirty visuals, in ascending order of distance to their root.
        /// </summary>
        /// <returns>A collection of visuals.</returns>
        public IEnumerator<IVisual> GetEnumerator()
        {
            foreach (var i in _inner)
            {
                foreach (var j in i.Value)
                {
                    yield return j;
                }
            }
        }

        /// <summary>
        /// Gets the dirty visuals, in ascending order of distance to their root.
        /// </summary>
        /// <returns>A collection of visuals.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}