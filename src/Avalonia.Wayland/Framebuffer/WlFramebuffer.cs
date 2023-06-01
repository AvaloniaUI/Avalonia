using System;
using Avalonia.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Framebuffer
{
    internal class WlFramebuffer : ILockedFramebuffer
    {
        private readonly WlSurface _wlSurface;
        private readonly WlBuffer _wlBuffer;

        public WlFramebuffer(WlSurface wlSurface, WlBuffer wlBuffer, IntPtr address, PixelSize size, int stride, double scale)
        {
            _wlSurface = wlSurface;
            _wlBuffer = wlBuffer;
            Address = address;
            Size = size;
            RowBytes = stride;
            Dpi = new Vector(96, 96) * scale;
            Format = PixelFormat.Bgra8888;
        }

        public void Dispose()
        {
            _wlSurface.Attach(_wlBuffer, 0, 0);
            _wlSurface.DamageBuffer(0, 0, Size.Width, Size.Height);
            _wlSurface.Commit();
        }

        public IntPtr Address { get; }

        public PixelSize Size { get; }

        public int RowBytes { get; }

        public Vector Dpi { get; }

        public PixelFormat Format { get; }
    }
}
