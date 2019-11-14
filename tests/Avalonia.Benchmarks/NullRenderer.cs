using System;
using System.Collections.Generic;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Avalonia.Benchmarks
{
    internal class NullRenderer : IRenderer
    {
        public bool DrawFps { get; set; }
        public bool DrawDirtyRects { get; set; }
        public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;

        public void AddDirty(IVisual visual)
        {
        }

        public void Dispose()
        {
        }

        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter) => null;

        public void Paint(Rect rect)
        {
        }

        public void RecalculateChildren(IVisual visual)
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
    }
}
