using System;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    class X11Framebuffer : ILockedFramebuffer
    {
        private readonly IntPtr _display;
        private readonly IntPtr _xid;
        private IUnmanagedBlob _blob;

        public X11Framebuffer(IntPtr display, IntPtr xid, int width, int height, int factor)
        {
            _display = display;
            _xid = xid;
            Width = width*factor;
            Height = height*factor;
            RowBytes = Width * 4;
            Dpi = new Vector(96, 96) * factor;
            Format = PixelFormat.Bgra8888;
            _blob = AvaloniaLocator.Current.GetService<IRuntimePlatform>().AllocBlob(RowBytes * Height);
            Address = _blob.Address;
        }
        
        public void Dispose()
        {
            var image = new X11.XImage();
            int bitsPerPixel = 32;
            image.width = Width;
            image.height = Height;
            image.format = 2; //ZPixmap;
            image.data = Address;
            image.byte_order = 0;// LSBFirst;
            image.bitmap_unit = bitsPerPixel;
            image.bitmap_bit_order = 0;// LSBFirst;
            image.bitmap_pad = bitsPerPixel;
            image.depth = 24;
            image.bytes_per_line = RowBytes - Width * 4;
            image.bits_per_pixel = bitsPerPixel;
            X11.XLockDisplay(_display);
            X11.XInitImage(ref image);
            var gc = X11.XCreateGC(_display, _xid, 0, IntPtr.Zero);
            X11.XPutImage(_display, _xid, gc, ref image, 0, 0, 0, 0, (uint) Width, (uint) Height);
            X11.XFreeGC(_display, gc);
            X11.XSync(_display, true);
            X11.XUnlockDisplay(_display);
            _blob.Dispose();
        }

        public IntPtr Address { get; }
        public int Width { get; }
        public int Height { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }
    }
}
