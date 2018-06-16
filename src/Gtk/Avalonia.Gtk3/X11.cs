using System;
using System.Runtime.InteropServices;

namespace Avalonia.Gtk3
{
    class X11
    {
        [DllImport("libX11.so.6")]
        public static extern IntPtr XInitThreads();
        
        [DllImport("libX11.so.6")]
        public static extern IntPtr XOpenDisplay(IntPtr name);
        
        [DllImport("libX11.so.6")]
        public static extern IntPtr XLockDisplay(IntPtr display);
        
        [DllImport("libX11.so.6")]
        public static extern IntPtr XUnlockDisplay(IntPtr display);
        
        [DllImport("libX11.so.6")]
        public static extern IntPtr XFreeGC(IntPtr display, IntPtr gc);
        
        [DllImport("libX11.so.6")]
        public static extern IntPtr XCreateGC(IntPtr display, IntPtr drawable, ulong valuemask, IntPtr values);
        
        [DllImport("libX11.so.6")]
        public static extern int XInitImage(ref XImage image);
        
        [DllImport("libX11.so.6")]
        public static extern int XDestroyImage(ref XImage image);
        
        [DllImport("libX11.so.6")]
        public static extern IntPtr XSetErrorHandler(XErrorHandler handler);

        [DllImport("libX11.so.6")]
        public static extern int XSync(IntPtr display, bool discard);

        public delegate int XErrorHandler(IntPtr display, ref XErrorEvent error);

        [DllImport("libX11.so.6")]
        public static extern int XPutImage(IntPtr display, IntPtr drawable, IntPtr gc, ref XImage image,
            int srcx, int srcy, int destx, int desty, uint width, uint height);

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct XErrorEvent
        {
            public int type;
            public IntPtr* display; /* Display the event was read from */
            public ulong serial; /* serial number of failed request */
            public byte error_code; /* error code of failed request */
            public byte request_code; /* Major op-code of failed request */
            public byte minor_code; /* Minor op-code of failed request */
            public IntPtr resourceid; /* resource id */
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct XImage
        {
            public int width, height; /* size of image */
            public int xoffset; /* number of pixels offset in X direction */
            public int format; /* XYBitmap, XYPixmap, ZPixmap */
            public IntPtr data; /* pointer to image data */
            public int byte_order; /* data byte order, LSBFirst, MSBFirst */
            public int bitmap_unit; /* quant. of scanline 8, 16, 32 */
            public int bitmap_bit_order; /* LSBFirst, MSBFirst */
            public int bitmap_pad; /* 8, 16, 32 either XY or ZPixmap */
            public int depth; /* depth of image */
            public int bytes_per_line; /* accelerator to next scanline */
            public int bits_per_pixel; /* bits per pixel (ZPixmap) */
            public ulong red_mask; /* bits in z arrangement */
            public ulong green_mask;
            public ulong blue_mask;
            private fixed byte funcs[128];
        }
        
        
    }
}