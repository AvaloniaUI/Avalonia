using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;
using SharpDX;
using SharpDX.Direct2D1;
using D2DBitmap = SharpDX.Direct2D1.Bitmap1;

namespace Avalonia.Direct2D1.Media.Imaging
{
    internal class D2DRenderTargetBitmapImpl : D2DBitmapImpl, IDrawingContextLayerImpl, ILayerFactory
    {
        private readonly BitmapRenderTarget _renderTarget;

        public D2DRenderTargetBitmapImpl(BitmapRenderTarget renderTarget)
            : base(renderTarget.Bitmap.QueryInterface<Bitmap1>())
        {
            _renderTarget = renderTarget;
        }

        public static D2DRenderTargetBitmapImpl CreateCompatible(
            SharpDX.Direct2D1.RenderTarget renderTarget,
            Size size)
        {
            var bitmapRenderTarget = new BitmapRenderTarget(
                renderTarget,
                CompatibleRenderTargetOptions.None,
                new Size2F((float)size.Width, (float)size.Height));
            return new D2DRenderTargetBitmapImpl(bitmapRenderTarget);
        }

        public IDrawingContextImpl CreateDrawingContext()
        {
            return new DrawingContextImpl( this, _renderTarget, null, () => Version++);
        }

        public bool IsCorrupted => false;

        public void Blit(IDrawingContextImpl context) => throw new NotSupportedException();

        public bool CanBlit => false;

        public IDrawingContextLayerImpl CreateLayer(Size size)
        {
            return CreateCompatible(_renderTarget, size);
        }

        public override void Dispose()
        {
            _renderTarget.Dispose();
        }

        public override OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target)
        {
            return new OptionalDispose<D2DBitmap>(_renderTarget.Bitmap.QueryInterface<Bitmap1>(), false);
        }

        public override void Save(Stream stream, int? quality = null)
        {
            using (var wic = new WicRenderTargetBitmapImpl(PixelSize, Dpi))
            {
                using (var dc = wic.CreateDrawingContext(null))
                {
                    dc.DrawBitmap(
                        this,
                        1,
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)),
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)));
                }

                wic.Save(stream);
            }
        }
    }
}
