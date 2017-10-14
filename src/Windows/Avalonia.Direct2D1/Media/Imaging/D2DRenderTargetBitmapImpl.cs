using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;
using DirectWriteFactory = SharpDX.DirectWrite.Factory;

namespace Avalonia.Direct2D1.Media.Imaging
{
    public class D2DRenderTargetBitmapImpl : D2DBitmapImpl, IRenderTargetBitmapImpl, ILayerFactory
    {
        private readonly DirectWriteFactory _dwriteFactory;
        private readonly BitmapRenderTarget _target;

        public D2DRenderTargetBitmapImpl(
            ImagingFactory imagingFactory,
            DirectWriteFactory dwriteFactory,
            BitmapRenderTarget target)
            : base(imagingFactory, target.Bitmap)
        {
            _dwriteFactory = dwriteFactory;
            _target = target;
        }

        public override int PixelWidth => _target.PixelSize.Width;
        public override int PixelHeight => _target.PixelSize.Height;

        public static D2DRenderTargetBitmapImpl CreateCompatible(
            ImagingFactory imagingFactory,
            DirectWriteFactory dwriteFactory,
            SharpDX.Direct2D1.RenderTarget renderTarget,
            Size size)
        {
            var bitmapRenderTarget = new BitmapRenderTarget(
                renderTarget,
                CompatibleRenderTargetOptions.None,
                new Size2F((float)size.Width, (float)size.Height));
            return new D2DRenderTargetBitmapImpl(imagingFactory, dwriteFactory, bitmapRenderTarget);
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(
                visualBrushRenderer,
                this,
                _target,
                _dwriteFactory,
                WicImagingFactory);
        }

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            return CreateCompatible(WicImagingFactory, _dwriteFactory, _target, size);
        }

        public override void Dispose()
        {
            _target.Dispose();
        }

        public override OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target)
        {
            return new OptionalDispose<D2DBitmap>(_target.Bitmap, false);
        }
    }
}
