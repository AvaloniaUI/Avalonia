using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1
{
    public class RenderTarget : IRenderTarget, ILayerFactory
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private readonly ID2D1RenderTarget _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        public RenderTarget(ID2D1RenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
        }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Platform.IDrawingContextImpl"/>.</returns>
        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, this, _renderTarget);
        }

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            return D2DRenderTargetBitmapImpl.CreateCompatible(_renderTarget, size);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }
    }
}
