using System;
using Avalonia.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
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
            Size = new PixelSize(width * factor, height * factor);
            RowBytes = width * 4;
            Dpi = new Vector(96, 96) * factor;
            Format = PixelFormat.Bgra8888;
            _blob = AvaloniaLocator.Current.GetService<IRuntimePlatform>().AllocBlob(RowBytes * height);
            Address = _blob.Address;
        }
        
        public void Dispose()
        {
            var image = new XImage();
            int bitsPerPixel = 32;
            image.width = Size.Width;
            image.height = Size.Height;
            image.format = 2; //ZPixmap;
            image.data = Address;
            image.byte_order = 0;// LSBFirst;
            image.bitmap_unit = bitsPerPixel;
            image.bitmap_bit_order = 0;// LSBFirst;
            image.bitmap_pad = bitsPerPixel;
            image.depth = 24;
            image.bytes_per_line = RowBytes - Size.Width * 4;
            image.bits_per_pixel = bitsPerPixel;
            XLockDisplay(_display);
            XInitImage(ref image);
            var gc = XCreateGC(_display, _xid, 0, IntPtr.Zero);
            XPutImage(_display, _xid, gc, ref image, 0, 0, 0, 0, (uint) Size.Width, (uint) Size.Height);
            XFreeGC(_display, gc);
            XSync(_display, true);
            XUnlockDisplay(_display);
            _blob.Dispose();
        }

        public IntPtr Address { get; }
        public PixelSize Size { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }
    }
}
