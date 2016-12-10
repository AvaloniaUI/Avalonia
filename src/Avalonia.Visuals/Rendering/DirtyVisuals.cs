using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    internal class DirtyVisuals : IEnumerable<IVisual>
    {
        private SortedDictionary<int, List<IVisual>> _inner = new SortedDictionary<int, List<IVisual>>();
        private Dictionary<IVisual, int> _index = new Dictionary<IVisual, int>();

        public int Count => _index.Count;

        public void Add(IVisual visual)
        {
            var distance = visual.IsAttachedToVisualTree ? visual.CalculateDistanceFromVisualRoot() : -1;
            int existingDistance;

            if (_index.TryGetValue(visual, out existingDistance))
            {
                if (distance == existingDistance)
                {
                    return;
                }

                _inner[existingDistance].Remove(visual);
                _index.Remove(visual);
            }

            List<IVisual> list;

            if (!_inner.TryGetValue(distance, out list))
            {
                list = new List<IVisual>();
                _inner.Add(distance, list);
            }

            list.Add(visual);
            _index.Add(visual, distance);
        }

        public void Clear()
        {
            _inner.Clear();
            _index.Clear();
        }

        public bool Remove(IVisual visual)
        {
            int distance;

            if (_index.TryGetValue(visual, out distance))
            {
                _inner[distance].Remove(visual);
                _index.Remove(visual);
                return true;
            }

            return false;
        }

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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
