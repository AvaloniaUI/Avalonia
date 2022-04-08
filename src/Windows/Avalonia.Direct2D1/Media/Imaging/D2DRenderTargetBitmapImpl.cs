using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;
using Vortice;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media.Imaging
{
    public class D2DRenderTargetBitmapImpl : D2DBitmapImpl, IDrawingContextLayerImpl, ILayerFactory
    {
        private readonly ID2D1BitmapRenderTarget _renderTarget;

        public D2DRenderTargetBitmapImpl(ID2D1BitmapRenderTarget renderTarget)
            : base(renderTarget.Bitmap)
        {
            _renderTarget = renderTarget;
        }

        public static D2DRenderTargetBitmapImpl CreateCompatible(
            ID2D1RenderTarget renderTarget,
            Size size)
        {
            var bitmapRenderTarget = renderTarget.CreateCompatibleRenderTarget(
                new Vortice.Mathematics.Size((float)size.Width, (float)size.Height)
                );
            return new D2DRenderTargetBitmapImpl(bitmapRenderTarget);
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, this, _renderTarget, null, () => Version++);
        }

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

        public override OptionalDispose<ID2D1Bitmap> GetDirect2DBitmap(ID2D1RenderTarget target)
        {
            return new OptionalDispose<ID2D1Bitmap>(_renderTarget.Bitmap, false);
        }

        public override void Save(Stream stream)
        {
            using (var wic = new WicRenderTargetBitmapImpl(PixelSize, Dpi))
            {
                using (var dc = wic.CreateDrawingContext(null))
                {
                    dc.DrawBitmap(
                        RefCountable.CreateUnownedNotClonable(this),
                        1,
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)),
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)));
                }

                wic.Save(stream);
            }
        }
    }
}
