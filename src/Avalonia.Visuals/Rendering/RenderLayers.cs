using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Rendering.SceneGraph;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class RenderLayers : IEnumerable<RenderLayer>
    {
        private readonly IRenderLayerFactory _factory;
        private List<RenderLayer> _inner = new List<RenderLayer>();
        private Dictionary<IVisual, RenderLayer> _index = new Dictionary<IVisual, RenderLayer>();

        public RenderLayers(IRenderLayerFactory factory)
        {
            _factory = factory;
        }

        public int Count => _inner.Count;
        public RenderLayer this[IVisual layerRoot] => _index[layerRoot];

        public RenderLayer Add(IVisual layerRoot, Size size)
        {
            RenderLayer result;

            if (!_index.TryGetValue(layerRoot, out result))
            {
                result = new RenderLayer(_factory, size, layerRoot);
                _inner.Add(result);
                _index.Add(layerRoot, result);
            }

            return result;
        }

        public bool Exists(IVisual layerRoot) => _index.ContainsKey(layerRoot);

        public void RemoveUnused(Scene scene)
        {
            for (var i = _inner.Count - 1; i >= 0; --i)
            {
                var layer = _inner[i];

                if (!scene.Layers.Exists(layer.LayerRoot))
                {
                    layer.Bitmap.Dispose();
                    _inner.RemoveAt(i);
                    _index.Remove(layer.LayerRoot);
                }
            }
        }

        public bool TryGetValue(IVisual layerRoot, out RenderLayer value)
        {
            return _index.TryGetValue(layerRoot, out value);
        }

        public IEnumerator<RenderLayer> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
