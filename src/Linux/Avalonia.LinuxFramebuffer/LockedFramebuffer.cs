using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer
{
    unsafe class LockedFramebuffer : ILockedFramebuffer
    {
        private readonly int _fb;
        private readonly fb_fix_screeninfo _fixedInfo;
        private fb_var_screeninfo _varInfo;
        private readonly IntPtr _address;

        public LockedFramebuffer(int fb, fb_fix_screeninfo fixedInfo, fb_var_screeninfo varInfo, IntPtr address, Vector dpi)
        {
            _fb = fb;
            _fixedInfo = fixedInfo;
            _varInfo = varInfo;
            _address = address;
            Dpi = dpi;
            //Use double buffering to avoid flicker
            Address = Marshal.AllocHGlobal(RowBytes * Size.Height);
        }


        void VSync()
        {
            NativeUnsafeMethods.ioctl(_fb, FbIoCtl.FBIO_WAITFORVSYNC, null);
        }

        public void Dispose()
        {
            VSync();
            NativeUnsafeMethods.memcpy(_address, Address, new IntPtr(RowBytes * Size.Height));

            Marshal.FreeHGlobal(Address);
            Address = IntPtr.Zero;
        }

        public IntPtr Address { get; private set; }
        public PixelSize Size => new PixelSize((int)_varInfo.xres, (int) _varInfo.yres);
        public int RowBytes => (int) _fixedInfo.line_length;
        public Vector Dpi { get; }
        public PixelFormat Format => _varInfo.bits_per_pixel == 16 ? PixelFormat.Rgb565 : _varInfo.blue.offset == 16 ? PixelFormat.Rgba8888 : PixelFormat.Bgra8888;
    }
}
