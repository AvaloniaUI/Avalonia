using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Direct2D1.Media;
using Avalonia.Direct2D1.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1
{
    class FramebufferShimRenderTarget : IRenderTarget
    {
        private readonly IFramebufferPlatformSurface _surface;
        private readonly ImagingFactory _imagingFactory;
        private readonly Factory _d2DFactory;
        private readonly SharpDX.DirectWrite.Factory _dwriteFactory;

        public FramebufferShimRenderTarget(IFramebufferPlatformSurface surface,
            ImagingFactory imagingFactory, Factory d2dFactory, SharpDX.DirectWrite.Factory dwriteFactory)
        {
            _surface = surface;
            _imagingFactory = imagingFactory;
            _d2DFactory = d2dFactory;
            _dwriteFactory = dwriteFactory;
        }

        public void Dispose()
        {
            
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var locked = _surface.Lock();
            if (locked.Format == PixelFormat.Rgb565)
            {
                locked.Dispose();
                throw new ArgumentException("Unsupported pixel format: " + locked.Format);
            }

            return new FramebufferShim(locked, _imagingFactory, _d2DFactory, _dwriteFactory)
                .CreateDrawingContext(visualBrushRenderer);
        }

        class FramebufferShim : WicRenderTargetBitmapImpl
        {
            private readonly ILockedFramebuffer _target;

            public FramebufferShim(ILockedFramebuffer target,
                ImagingFactory imagingFactory, Factory d2dFactory, SharpDX.DirectWrite.Factory dwriteFactory
                ) : base(imagingFactory, d2dFactory, dwriteFactory,
                    target.Width, target.Height, target.Dpi.X, target.Dpi.Y, target.Format)
            {
                _target = target;
            }
            
            public override IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
            {
                return base.CreateDrawingContext(visualBrushRenderer, () =>
                {
                    using (var l = WicImpl.Lock(BitmapLockFlags.Read))
                    {
                        for (var y = 0; y < _target.Height; y++)
                        {
                            UnmanagedMethods.CopyMemory(
                                (_target.Address + _target.RowBytes * y),
                                (l.Data.DataPointer + l.Stride * y),
                                (UIntPtr)Math.Min(l.Stride, _target.RowBytes));
                        }
                    }
                    Dispose();
                    _target.Dispose();

                });
            }
        }

    }
}
