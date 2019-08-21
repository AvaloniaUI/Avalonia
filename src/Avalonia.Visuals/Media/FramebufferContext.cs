using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Media
{
    public class FramebufferContext : IDisposable
    {
        private readonly byte _bytesPerPixel;
        private readonly int _byteCount;

        public FramebufferContext(IFramebufferSurface surface)
        {
            Framebuffer = surface.Lock();
            _bytesPerPixel = GetBytesPerPixel(Framebuffer.Format);
            _byteCount = Framebuffer.RowBytes * Framebuffer.Size.Height;
        }

        private static byte GetBytesPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Rgb565:
                    return 3;
                case PixelFormat.Rgba8888:
                case PixelFormat.Bgra8888:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null);
            }
        }

        public ILockedFramebuffer Framebuffer { get; }

        public Span<byte> GetPixels()
        {
            unsafe
            {
                return new Span<byte>((byte*)Framebuffer.Address, _byteCount);
            }
        }

        public Span<byte> GetPixel(int x, int y)
        {
            unsafe
            {
                var zero = (byte*)Framebuffer.Address;
                var offset = Framebuffer.RowBytes * y + _bytesPerPixel * x;
                return new Span<byte>(zero + offset, _bytesPerPixel);
            }
        }

        public void Dispose()
        {
            Framebuffer.Dispose();
        }
    }
}
