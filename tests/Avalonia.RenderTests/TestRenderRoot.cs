using Avalonia.Rendering;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;


namespace Avalonia.Skia.RenderTests
{
    public class TestRenderRoot : Decorator, IPresentationSource, IInputRoot, ILayoutRoot
    {
        private readonly IRenderTarget _renderTarget;
        public Size ClientSize { get; private set; }
        internal CompositingRenderer Renderer { get; private set; } = null!;
        IRenderer IPresentationSource.Renderer => Renderer;
        IHitTester IPresentationSource.HitTester => new NullHitTester();
        public IInputRoot InputRoot => this;

        ILayoutRoot IPresentationSource.LayoutRoot => this;

        public double LayoutScaling => 1l;

        public ILayoutManager LayoutManager { get; }

        Layoutable ILayoutRoot.RootVisual => this;

        public Visual? RootVisual => this;
        public double RenderScaling { get; }

        public TestRenderRoot(double scaling, IRenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
            RenderScaling = scaling;
            LayoutManager = new LayoutManager(this);

        }
        
        class NullHitTester : IHitTester
        {
            public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool>? filter) => Array.Empty<Visual>();

            public Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool>? filter) => null;
        }

        internal void Initialize(CompositingRenderer renderer, Control child)
        {
            Renderer = renderer;
            SetPresentationSourceForRootVisual(this);
            Renderer.CompositionTarget.Root = this.CompositionVisual;
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
        
        public IFocusManager? FocusManager { get; }
        public IPlatformSettings? PlatformSettings { get; }
        public IInputElement? PointerOverElement { get; set; }
        public ITextInputMethodImpl? InputMethod { get; }
        public InputElement RootElement { get; }
        
    }
}
