using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Platform;
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

        public RenderLayer Add(IVisual layerRoot, Size size)
        {
            RenderLayer result;

            if (!_index.TryGetValue(layerRoot, out result))
            {
                var bitmap = _factory.CreateLayer(layerRoot, size);
                result = new RenderLayer(bitmap, size, layerRoot);
                _inner.Add(result);
                _index.Add(layerRoot, result);
            }

            return result;
        }

        public RenderLayer Get(IVisual layerRoot)
        {
            RenderLayer result;
            _index.TryGetValue(layerRoot, out result);
            return result;
        }

        public void RemoveUnused(Scene scene)
        {
            for (var i = _inner.Count - 1; i >= 0; --i)
            {
                var layer = _inner[i];
                var node = (VisualNode)scene.FindNode(layer.LayerRoot);

                if (node == null || node.LayerRoot != layer.LayerRoot)
                {
                    layer.Bitmap.Dispose();
                    _inner.RemoveAt(i);
                    _index.Remove(layer.LayerRoot);
                }
            }
        }

        public IEnumerator<RenderLayer> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
