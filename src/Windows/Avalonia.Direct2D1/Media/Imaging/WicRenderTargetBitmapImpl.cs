using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal class WicRenderTargetBitmapImpl : WicBitmapImpl, IDrawingContextLayerImpl
    {
        private readonly WicRenderTarget _renderTarget;

        public WicRenderTargetBitmapImpl(
            PixelSize size,
            Vector dpi,
            Platform.PixelFormat? pixelFormat = null)
            : base(size, dpi, pixelFormat)
        {
            var props = new RenderTargetProperties
            {
                DpiX = (float)dpi.X,
                DpiY = (float)dpi.Y,
            };

            _renderTarget = new WicRenderTarget(
                Direct2D1Platform.Direct2D1Factory,
                WicImpl,
                props);
        }

        public override void Dispose()
        {
            _renderTarget.Dispose();

            base.Dispose();
        }

        public virtual IDrawingContextImpl CreateDrawingContext()
            => CreateDrawingContext(null);

        public bool IsCorrupted => false;

        public IDrawingContextImpl CreateDrawingContext(Action finishedCallback)
        {
            return new DrawingContextImpl(null, _renderTarget, finishedCallback: () =>
                {
                    Version++;
                    finishedCallback?.Invoke();
                });
        }

        public void Blit(IDrawingContextImpl context) => throw new NotSupportedException();
        public bool CanBlit => false;
    }
}
