using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using Gdk;
using Gtk;
#if WIN32
using Avalonia.Win32.Interop;
#endif

namespace Avalonia.Skia
{
    internal partial class RenderTarget : IRenderTarget
    {
        public SKSurface Surface { get; protected set; }

        public virtual DrawingContext CreateDrawingContext()
        {
            return
                new DrawingContext(
                    new DrawingContextImpl(Surface.Canvas));
        }

        public void Dispose()
        {
            // Nothing to do here.
        }
    }

    internal class WindowRenderTarget : RenderTarget
    {
        //private readonly IPlatformHandle _hwnd;
		private readonly Gtk.Window _hwnd;
        SKBitmap _bitmap;
        int Width { get; set; }
        int Height { get; set; }

        public WindowRenderTarget(IPlatformHandle hwnd)
        {
			_hwnd = (Gtk.Window) hwnd;
            FixSize();
        }

        private void FixSize()
        {
            int width, height;
            GetPlatformWindowSize(out width, out height);
            /*if (Width == width && Height == height)
                return;*/

            Width = width;
            Height = height;

            if (Surface != null)
            {
                Surface.Dispose();
            }

            if (_bitmap != null)
            {
                _bitmap.Dispose();
            }

            _bitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            IntPtr length;
            var pixels = _bitmap.GetPixels(out length);

            // Wrap the bitmap in a Surface and keep it cached
            //Surface = SKSurface.Create(_bitmap.Info, pixels, _bitmap.RowBytes);
			Surface = SKSurface.Create (_bitmap.Info.Width, _bitmap.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul, pixels, _bitmap.RowBytes);
        }

        private void GetPlatformWindowSize(out int w, out int h)
        {
#if WIN32
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd.Handle, out rc);
            w = rc.right - rc.left;
            h = rc.bottom - rc.top;
#else
			w = 800;
			h = 600;
#endif
        }

#if WIN32
        private Size GetWindowDpiWin32()
        {
            if (UnmanagedMethods.ShCoreAvailable)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _hwnd.Handle,
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
#endif

        public override DrawingContext CreateDrawingContext()
        {
            FixSize();

            var canvas = Surface.Canvas;
            canvas.RestoreToCount(0);
            canvas.Save();
            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();

            double scale = 1.0;

            var runtimeService = AvaloniaLocator.Current.GetService<IRuntimePlatform>();

            if (runtimeService != null)
            {
                switch (runtimeService.GetRuntimeInfo().OperatingSystem)
                {
                    case OperatingSystemType.WinNT:
					#if WIN32
                        var dpi = GetWindowDpiWin32();
                        scale = dpi.Width / 96.0;
					#endif
                        break;
                }
            }

            var result =
                new DrawingContext(
                    new WindowDrawingContextImpl(this), Matrix.CreateScale(scale, scale));
            
            return result;
        }

        public void Present()
        {
            _bitmap.LockPixels();
            IntPtr length;
            var pixels = _bitmap.GetPixels(out length);

#if WIN32
            UnmanagedMethods.BITMAPINFO bmi = new UnmanagedMethods.BITMAPINFO();
            bmi.biSize = UnmanagedMethods.SizeOf_BITMAPINFOHEADER;
            bmi.biWidth = _bitmap.Width;
            bmi.biHeight = -_bitmap.Height; // top-down image
            bmi.biPlanes = 1;
            bmi.biBitCount = 32;
            bmi.biCompression = (uint)UnmanagedMethods.BitmapCompressionMode.BI_RGB;
            bmi.biSizeImage = 0;

            IntPtr hdc = UnmanagedMethods.GetDC(_hwnd.Handle);

            int ret = UnmanagedMethods.SetDIBitsToDevice(hdc,
                0, 0,
                (uint)_bitmap.Width, (uint)_bitmap.Height,
                0, 0,
                0, (uint)_bitmap.Height,
                pixels,
                ref bmi,
                (uint)UnmanagedMethods.DIBColorTable.DIB_RGB_COLORS);

            UnmanagedMethods.ReleaseDC(_hwnd.Handle, hdc);
#else
            //throw new NotImplementedException();
#endif

			var gdk = _hwnd.GdkWindow;

			Gdk.GC gc = new Gdk.GC ((Drawable)gdk);

			// Is the _bitmap.Bytes[] a reference or will this be a copy??? its a byte[] array
			global::Gdk.Pixbuf pb = new Pixbuf (_bitmap.Bytes, Gdk.Colorspace.Rgb, true, 8, _bitmap.Width, _bitmap.Height, _bitmap.RowBytes);
			gdk.DrawPixbuf (gc, pb, 0, 0, 0, 0, _bitmap.Width, _bitmap.Height, RgbDither.None, 0, 0);

			_bitmap.UnlockPixels();

        }
    }
}