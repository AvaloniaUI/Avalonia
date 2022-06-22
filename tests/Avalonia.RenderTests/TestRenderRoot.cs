using Avalonia.Rendering;
using System.Threading.Tasks;
using System;
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
        public IRenderer Renderer { get; private set; }
        public double RenderScaling { get; }

        public TestRenderRoot(double scaling, IRenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
            RenderScaling = scaling;
        }

        public void Initialize(IRenderer renderer, Control child)
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