using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Benchmarks
{
    internal class NullRenderer : IRenderer
    {
        public bool DrawFps { get; set; }
        public bool DrawDirtyRects { get; set; }
#pragma warning disable CS0067
        public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;
#pragma warning restore CS0067
        public void AddDirty(Visual visual)
        {
        }

        public void Dispose()
        {
        }

        public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter) => null;

        public Visual HitTestFirst(Point p, Visual root, Func<Visual, bool> filter) => null;

        public void Paint(Rect rect)
        {
        }

        public void RecalculateChildren(Visual visual)
        {
        }

        public void Resized(Size size)
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public ValueTask<object> TryGetRenderInterfaceFeature(Type featureType) => new(0);
    }
}
