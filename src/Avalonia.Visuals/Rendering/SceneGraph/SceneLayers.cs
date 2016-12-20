using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class SceneLayers : IEnumerable<SceneLayer>
    {
        private List<SceneLayer> _inner = new List<SceneLayer>();
        private Dictionary<IVisual, SceneLayer> _index = new Dictionary<IVisual, SceneLayer>();

        public SceneLayers()
        {
        }

        public int Count => _inner.Count;

        public bool HasDirty
        {
            get
            {
                foreach (var layer in _inner)
                {
                    if (!layer.Dirty.IsEmpty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public SceneLayer this[int index] => _inner[index];
        public SceneLayer this[IVisual visual] => _index[visual];

        public SceneLayer Add(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            var layer = new SceneLayer(layerRoot);
            var insert = FindInsertIndex(layer);
            _index.Add(layerRoot, layer);
            _inner.Insert(insert, layer);
            return layer;
        }

        public SceneLayers Clone()
        {
            var result = new SceneLayers();

            foreach (var src in _inner)
            {
                var dest = src.Clone();
                result._index.Add(dest.LayerRoot, dest);
                result._inner.Add(dest);
            }

            return result;
        }

        public bool Exists(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            return _index.ContainsKey(layerRoot);
        }

        public SceneLayer Find(IVisual layerRoot)
        {
            SceneLayer result;
            _index.TryGetValue(layerRoot, out result);
            return result;
        }

        public SceneLayer GetOrAdd(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            SceneLayer result;

            if (!_index.TryGetValue(layerRoot, out result))
            {
                result = Add(layerRoot);
            }

            return result;
        }

        public bool Remove(IVisual layerRoot)
        {
            Contract.Requires<ArgumentNullException>(layerRoot != null);

            SceneLayer layer;

            if (_index.TryGetValue(layerRoot, out layer))
            {
                _index.Remove(layerRoot);
                _inner.Remove(layer);
            }

            return layer != null;
        }

        public IEnumerator<SceneLayer> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int FindInsertIndex(SceneLayer insert)
        {
            var index = 0;

            foreach (var layer in _inner)
            {
                if (layer.DistanceFromRoot > insert.DistanceFromRoot)
                {
                    break;
                }

                ++index;
            }

            return index;
        }
    }
}
