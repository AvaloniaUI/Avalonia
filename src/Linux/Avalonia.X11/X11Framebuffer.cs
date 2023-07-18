using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Platform.Internal;
using SkiaSharp;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    internal class X11Framebuffer : ILockedFramebuffer
    {
        private readonly IntPtr _display;
        private readonly IntPtr _xid;
        private readonly int _depth;
        private UnmanagedBlob _blob;

        public X11Framebuffer(IntPtr display, IntPtr xid, int depth, int width, int height, double factor)
        {
            // HACK! Please fix renderer, should never ask for 0x0 bitmap.
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            _display = display;
            _xid = xid;
            _depth = depth;
            Size = new PixelSize(width, height);
            RowBytes = width * 4;
            Dpi = new Vector(96, 96) * factor;
            Format = PixelFormat.Bgra8888;
            _blob = new UnmanagedBlob(RowBytes * height);
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
            image.depth = _depth;
            image.bytes_per_line = RowBytes;
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
