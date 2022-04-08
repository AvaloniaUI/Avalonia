using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class WicRenderTargetBitmapImpl : WicBitmapImpl, IDrawingContextLayerImpl
    {
        private readonly ID2D1RenderTarget _renderTarget;

        public WicRenderTargetBitmapImpl(
            PixelSize size,
            Vector dpi,
            PixelFormat? pixelFormat = null)
            : base(size, dpi, pixelFormat)
        {
            var props = new RenderTargetProperties
            {
                DpiX = (float)dpi.X,
                DpiY = (float)dpi.Y,
            };

            _renderTarget = Direct2D1Platform.Direct2D1Factory.CreateWicBitmapRenderTarget(
                WicImpl,
                props);
        }

        public override void Dispose()
        {
            _renderTarget.Dispose();

            base.Dispose();
        }

        public virtual IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            => CreateDrawingContext(visualBrushRenderer, null);

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer, Action finishedCallback)
        {
            return new DrawingContextImpl(visualBrushRenderer, null, _renderTarget, finishedCallback: () =>
                {
                    Version++;
                    finishedCallback?.Invoke();
                });
        }

        public void Blit(IDrawingContextImpl context) => throw new NotSupportedException();
        public bool CanBlit => false;
    }
}
