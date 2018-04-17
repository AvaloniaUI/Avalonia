using System;

namespace Avalonia.Platform
{
    public class LockedFramebuffer : ILockedFramebuffer
    {
        private readonly Action _onDispose;

        public LockedFramebuffer(IntPtr address, int width, int height, int rowBytes, Vector dpi, PixelFormat format,
            Action onDispose)
        {
            _onDispose = onDispose;
            Address = address;
            Width = width;
            Height = height;
            RowBytes = rowBytes;
            Dpi = dpi;
            Format = format;
        }

        public IntPtr Address { get; }
        public int Width { get; }
        public int Height { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}