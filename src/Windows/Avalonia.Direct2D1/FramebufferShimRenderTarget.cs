#nullable enable

using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Direct2D1.Media;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using SharpDX.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1
{
    class FramebufferShimRenderTarget : IRenderTarget
    {
        private IFramebufferRenderTarget? _target;

        public FramebufferShimRenderTarget(IFramebufferPlatformSurface surface)
        {
            _target = surface.CreateFramebufferRenderTarget();
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
        }

        public IDrawingContextImpl CreateDrawingContext()
        {
            if (_target == null)
                throw new ObjectDisposedException(nameof(FramebufferShimRenderTarget));
            var locked = _target.Lock();
            if (locked.Format == PixelFormat.Rgb565)
            {
                locked.Dispose();
                throw new ArgumentException("Unsupported pixel format: " + locked.Format);
            }

            return new FramebufferShim(locked)
                .CreateDrawingContext();
        }

        public bool IsCorrupted => false;

        class FramebufferShim : WicRenderTargetBitmapImpl
        {
            private readonly ILockedFramebuffer _target;

            public FramebufferShim(ILockedFramebuffer target) : 
                base(target.Size, target.Dpi, target.Format)
            {
                _target = target;
            }
            
            public override IDrawingContextImpl CreateDrawingContext()
            {
                return base.CreateDrawingContext(() =>
                {
                    using (var l = WicImpl.Lock(BitmapLockFlags.Read))
                    {
                        for (var y = 0; y < _target.Size.Height; y++)
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
