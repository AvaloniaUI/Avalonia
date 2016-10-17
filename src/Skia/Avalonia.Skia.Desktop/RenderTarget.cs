using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
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
        private readonly IPlatformHandle _hwnd;
        SKBitmap _bitmap;
        int Width { get; set; }
        int Height { get; set; }

        public WindowRenderTarget(IPlatformHandle hwnd)
        {
            _hwnd = hwnd;
            FixSize();
        }

        private void FixSize()
        {
            int width, height;
            GetPlatformWindowSize(out width, out height);
            if (Width == width && Height == height)
                return;

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
            Surface = SKSurface.Create(_bitmap.Info, pixels, _bitmap.RowBytes);
        }

        private void GetPlatformWindowSize(out int w, out int h)
        {
#if WIN32
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd.Handle, out rc);
            w = rc.right - rc.left;
            h = rc.bottom - rc.top;
#else
			throw new NotImplementedException();
#endif
        }

#if WIN32
        private Size GetWindowDpi()
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
                        var dpi = GetWindowDpi();
                        scale = dpi.Width / 96.0;
                        break;
                }
            }
                        
            var result =
                new DrawingContext(
                    new WindowDrawingContextImpl(this));

            result.PushPreTransform(Matrix.CreateScale(scale, scale));
            result.PushTransformContainer();

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
            throw new NotImplementedException();
#endif

            _bitmap.UnlockPixels();
        }
    }
}