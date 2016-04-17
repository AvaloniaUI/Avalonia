using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;
using SkiaSharp;

// TODO: I'm not sure the best way to bring in the platform specific rendering
//
#if __IOS__
using CoreGraphics;
using UIKit;
#elif WIN32
using Perspex.Win32.Interop;
#endif

namespace Perspex.Skia
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
        private readonly IntPtr _hwnd;
        SKBitmap _bitmap;

        int Width { get; set; }
        int Height { get; set; }

        public WindowRenderTarget(IntPtr hwnd)
        {
            _hwnd = hwnd;
            FixSize();
        }

#if __IOS__
        private CGRect GetApplicationFrame()
        {
            // if we are excluding Status Bar then we use ApplicationFrame
            // otherwise we use full screen bounds. Note that this must also match
            // the Skia/PerspexView!!!
            //
            bool excludeStatusArea = false; // TODO: make this configurable later
            if (excludeStatusArea)
            {
                return UIScreen.MainScreen.ApplicationFrame;
            }
            else
            {
                return UIScreen.MainScreen.Bounds;
            }
        }
#endif

        private void FixSize()
        {
            int width, height;
            GetPlatformWindowSize(_hwnd, out width, out height);
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

            _bitmap = new SKBitmap(width, height, SKColorType.N_32, SKAlphaType.Premul);

            IntPtr length;
            var pixels = _bitmap.GetPixels(out length);

            // Wrap the bitmap in a Surface and keep it cached
            Surface = SKSurface.Create(_bitmap.Info, pixels, _bitmap.RowBytes);
        }

        private void GetPlatformWindowSize(IntPtr hwnd, out int w, out int h)
        {
#if __IOS__
            var bounds = GetApplicationFrame();
            w = (int)bounds.Width;
            h = (int)bounds.Height;

#elif WIN32
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd, out rc);
            w = rc.right - rc.left;
            h = rc.bottom - rc.top;
#else
			throw new NotImplementedException();
#endif
        }

        public override DrawingContext CreateDrawingContext()
        {
            FixSize();

            var canvas = Surface.Canvas;
            canvas.RestoreToCount(0);
            canvas.Save();

#if __IOS__
            var screenScale = UIScreen.MainScreen.Scale;
            canvas.Scale((float)screenScale, (float)screenScale);
#endif

            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();

            return
                new DrawingContext(
                    new WindowDrawingContextImpl(this));
        }

        public void Present()
        {
            _bitmap.LockPixels();
            IntPtr length;
            var pixels = _bitmap.GetPixels(out length);

#if __IOS__
            const int bitmapInfo = ((int)CGBitmapFlags.ByteOrder32Big) | ((int)CGImageAlphaInfo.PremultipliedLast);
            var bounds = GetApplicationFrame();
            var statusBarOffset = UIScreen.MainScreen.Bounds.Height - bounds.Height;

            using (var colorSpace = CGColorSpace.CreateDeviceRGB())
            using (var bContext = new CGBitmapContext(pixels, _bitmap.Width, _bitmap.Height, 8, _bitmap.Width * 4, colorSpace, (CGImageAlphaInfo)bitmapInfo))
            using (var image = bContext.ToImage())
            using (var context = UIGraphics.GetCurrentContext())
            {
                // flip the image for CGContext.DrawImage
                context.TranslateCTM(0, bounds.Height + statusBarOffset);
                context.ScaleCTM(1, -1);
                context.DrawImage(bounds, image);
            }

#elif WIN32
            UnmanagedMethods.BITMAPINFO bmi = new UnmanagedMethods.BITMAPINFO();
            bmi.biSize = UnmanagedMethods.SizeOf_BITMAPINFOHEADER;
            bmi.biWidth = _bitmap.Width;
            bmi.biHeight = -_bitmap.Height; // top-down image
            bmi.biPlanes = 1;
            bmi.biBitCount = 32;
            bmi.biCompression = (uint)UnmanagedMethods.BitmapCompressionMode.BI_RGB;
            bmi.biSizeImage = 0;

            IntPtr hdc = UnmanagedMethods.GetDC(_hwnd);

            int ret = UnmanagedMethods.SetDIBitsToDevice(hdc,
                0, 0,
                (uint)_bitmap.Width, (uint)_bitmap.Height,
                0, 0,
                0, (uint)_bitmap.Height,
                pixels,
                ref bmi,
                (uint)UnmanagedMethods.DIBColorTable.DIB_RGB_COLORS);

            UnmanagedMethods.ReleaseDC(_hwnd, hdc);
#endif

            _bitmap.UnlockPixels();
        }

    }
}
