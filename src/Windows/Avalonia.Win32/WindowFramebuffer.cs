using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Win32.Interop;
using PixelFormat = Avalonia.Controls.Platform.Surfaces.PixelFormat;

namespace Avalonia.Win32
{
    public class WindowFramebuffer : ILockedFramebuffer
    {
        private readonly IntPtr _handle;
        private IntPtr _pBitmap;
        private UnmanagedMethods.BITMAPINFOHEADER _bmpInfo;

        public WindowFramebuffer(IntPtr handle, int width, int height)
        {
            
            if (width <= 0)
                throw new ArgumentException("width is less than zero");
            if (height <= 0)
                throw new ArgumentException("height is less than zero");
            _handle = handle;
            _bmpInfo.Init();
            _bmpInfo.biPlanes = 1;
            _bmpInfo.biBitCount = 32;
            _bmpInfo.Init();
            _bmpInfo.biWidth = width;
            _bmpInfo.biHeight = -height;
            _pBitmap = Marshal.AllocHGlobal(width * height * 4);
        }

        ~WindowFramebuffer()
        {
            Deallocate();
        }

        public IntPtr Address => _pBitmap;
        public int RowBytes => Width * 4;
        public PixelFormat Format => PixelFormat.Bgra8888;

        public Size Dpi
        {
            get
            {
                if (UnmanagedMethods.ShCoreAvailable)
                {
                    uint dpix, dpiy;

                    var monitor = UnmanagedMethods.MonitorFromWindow(_handle,
                        UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                    if (UnmanagedMethods.GetDpiForMonitor(
                            monitor,
                            UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                            out dpix,
                            out dpiy) == 0)
                    {
                        return new Size(dpix, dpiy);
                    }
                }
                return new Size(96, 96);
            }
        }

        public int Width => _bmpInfo.biWidth;

        public int Height => -_bmpInfo.biHeight;

        public void DrawToDevice(IntPtr hDC, int destX = 0, int destY = 0, int srcX = 0, int srcY = 0, int width = -1,
            int height = -1)
        {
            if(_pBitmap == IntPtr.Zero)
                throw new ObjectDisposedException("Framebuffer");
            if (width == -1)
                width = Width;
            if (height == -1)
                height = Height;
            UnmanagedMethods.SetDIBitsToDevice(hDC, destX, destY, (uint) width, (uint) height, srcX, srcY,
                0, (uint)Height, _pBitmap, ref _bmpInfo, 0);
        }

        public bool DrawToWindow(IntPtr hWnd, int destX = 0, int destY = 0, int srcX = 0, int srcY = 0, int width = -1,
            int height = -1)
        {

            if (_pBitmap == IntPtr.Zero)
                throw new ObjectDisposedException("Framebuffer");
            if (hWnd == IntPtr.Zero)
                return false;
            IntPtr hDC = UnmanagedMethods.GetDC(hWnd);
            if (hDC == IntPtr.Zero)
                return false;
            DrawToDevice(hDC, destX, destY, srcX, srcY, width, height);
            UnmanagedMethods.ReleaseDC(hWnd, hDC);
            return true;
        }

        public void Dispose()
        {
            //It's not an *actual* dispose. This call means "We are done drawing"
            DrawToWindow(_handle);
        }

        public void Deallocate()
        {
            if (_pBitmap != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_pBitmap);
                _pBitmap = IntPtr.Zero;
            }
        }
    }
}