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
        private List<RenderLayer> _inner = new List<RenderLayer>();
        private Dictionary<IVisual, RenderLayer> _index = new Dictionary<IVisual, RenderLayer>();
        private IRenderTarget _target;

        public int Count => _inner.Count;
        public RenderLayer this[IVisual layerRoot] => _index[layerRoot];

        public RenderLayers()
        {
        }

        public RenderLayers(IRenderTarget target)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            _target = target;
        }

        public void SetTarget(IRenderTarget target)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            _target = target;
            Clear();
        }

        public void Clear()
        {
            foreach (var i in _inner)
            {
                i.Bitmap.Dispose();
            }

            _inner.Clear();
            _index.Clear();
        }

        public void Update(Scene scene)
        {
            for (var i = scene.Layers.Count - 1; i >= 0; --i)
            {
                var src = scene.Layers[i];
                RenderLayer layer;

                if (!_index.TryGetValue(src.LayerRoot, out layer))
                {
                    layer = new RenderLayer(_target, scene.Size, scene.Scaling, src.LayerRoot);
                    _inner.Add(layer);
                    _index.Add(src.LayerRoot, layer);
                }
                else
                {
                    layer.ResizeBitmap(scene.Size, scene.Scaling);
                }
            }

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
