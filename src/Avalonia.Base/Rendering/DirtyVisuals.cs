using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Stores a list of dirty visuals for an <see cref="IRenderer"/>.
    /// </summary>
    /// <remarks>
    /// This class stores the dirty visuals for a scene, ordered by their distance to the root
    /// visual. TODO: We probably want to put an upper limit on the number of visuals that can be
    /// stored and if we reach that limit, assume all visuals are dirty.
    /// </remarks>
    internal class DirtyVisuals : IEnumerable<Visual>
    {
        private SortedDictionary<int, List<Visual>> _inner = new SortedDictionary<int, List<Visual>>();
        private Dictionary<Visual, int> _index = new Dictionary<Visual, int>();
        private int _enumerating;

        /// <summary>
        /// Gets the number of dirty visuals.
        /// </summary>
        public int Count => _index.Count;

        /// <summary>
        /// Adds a visual to the dirty list.
        /// </summary>
        /// <param name="visual">The dirty visual.</param>
        public void Add(Visual visual)
        {
            if (_enumerating > 0)
            {
                throw new InvalidOperationException("Visual was invalidated during a render pass");
            }

            var distance = visual.CalculateDistanceFromAncestor((Visual?)visual.GetVisualRoot());

            if (_index.TryGetValue(visual, out var existingDistance))
            {
                if (distance == existingDistance)
                {
                    return;
                }

                _inner[existingDistance].Remove(visual);
                _index.Remove(visual);
            }

            if (!_inner.TryGetValue(distance, out var list))
            {
                list = new List<Visual>();
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
            if (_enumerating > 0)
            {
                throw new InvalidOperationException("Cannot clear while enumerating");
            }

            _inner.Clear();
            _index.Clear();
        }

        /// <summary>
        /// Gets the dirty visuals, in ascending order of distance to their root.
        /// </summary>
        /// <returns>A collection of visuals.</returns>
        public IEnumerator<Visual> GetEnumerator()
        {
            _enumerating++;
            try
            {
                foreach (var i in _inner)
                {
                    foreach (var j in i.Value)
                    {
                        yield return j;
                    }
                }
            }
            finally
            {
                _enumerating--;
            }
        }
        
        /// <summary>
        /// Gets the dirty visuals, in ascending order of distance to their root.
        /// </summary>
        /// <returns>A collection of visuals.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
