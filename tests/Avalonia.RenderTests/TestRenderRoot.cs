using Avalonia.Rendering;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform;


#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class TestRenderRoot : Decorator, IRenderRoot
    {
        private readonly IRenderTarget _renderTarget;
        public Size ClientSize { get; private set; }
        internal IRenderer Renderer { get; private set; }
        IRenderer IRenderRoot.Renderer => Renderer;
        IHitTester IRenderRoot.HitTester => new NullHitTester();
        public double RenderScaling { get; }

        public TestRenderRoot(double scaling, IRenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
            RenderScaling = scaling;
        }
        
        class NullHitTester : IHitTester
        {
            public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter) => Array.Empty<Visual>();

            public Visual HitTestFirst(Point p, Visual root, Func<Visual, bool> filter) => null;
        }

        internal void Initialize(IRenderer renderer, Control child)
        {
            Renderer = renderer;
            Child = child;
            Width = child.Width;
            Height = child.Height;
            ClientSize = new Size(Width, Height);
            Measure(ClientSize);
            Arrange(new Rect(ClientSize));
        }

        public IRenderTarget CreateRenderTarget() => _renderTarget;

        public void Invalidate(Rect rect)
        {
        }

        public Point PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

        public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);
    }
}