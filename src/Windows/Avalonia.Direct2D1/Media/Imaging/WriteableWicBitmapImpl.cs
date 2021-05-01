using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1.Media.Imaging
{
    class WriteableWicBitmapImpl : WicBitmapImpl, IWriteableBitmapImpl
    {
        public WriteableWicBitmapImpl(PixelSize size, Vector dpi, PixelFormat? pixelFormat, AlphaFormat? alphaFormat) 
            : base(size, dpi, pixelFormat, alphaFormat)
        {
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            if (WicImpl.PixelFormat != SharpDX.WIC.PixelFormat.Format32bppPBGRA &&
                WicImpl.PixelFormat != SharpDX.WIC.PixelFormat.Format32bppPRGBA)
                throw new NotSupportedException("Direct2D only supports drawing to bitmaps with premultiplied alpha.");

            var renderTarget = new WicRenderTarget(
                Direct2D1Platform.Direct2D1Factory,
                WicImpl,
                new RenderTargetProperties
                {
                    DpiX = (float)Dpi.X,
                    DpiY = (float)Dpi.Y,
                });

            return new DrawingContextImpl(visualBrushRenderer, null, renderTarget, finishedCallback: () =>
            {
                Version++;
            });
        }
 
        class LockedBitmap : ILockedFramebuffer
        {
            private readonly WriteableWicBitmapImpl _parent;
            private readonly BitmapLock _lock;
            private readonly PixelFormat _format;

            public LockedBitmap(WriteableWicBitmapImpl parent, BitmapLock l, PixelFormat format)
            {
                _parent = parent;
                _lock = l;
                _format = format;
            }


            public void Dispose()
            {
                _lock.Dispose();
                _parent.Version++;
            }

            public IntPtr Address => _lock.Data.DataPointer;
            public PixelSize Size => _lock.Size.ToAvalonia();
            public int RowBytes => _lock.Stride;
            public Vector Dpi { get; } = new Vector(96, 96);
            public PixelFormat Format => _format;

        }

        public ILockedFramebuffer Lock() =>
            new LockedBitmap(this, WicImpl.Lock(BitmapLockFlags.Write), PixelFormat.Value);
    }
}
