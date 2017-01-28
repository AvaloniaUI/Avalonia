using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using CoreGraphics;
using UIKit;
using Avalonia.Rendering;

namespace Avalonia.Skia
{
    internal partial class RenderTarget : IRenderTarget
    {
        public SKSurface Surface { get; protected set; }

        public virtual IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(Surface.Canvas);
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

        private CGRect GetApplicationFrame()
        {
            // if we are excluding Status Bar then we use ApplicationFrame
            // otherwise we use full screen bounds. Note that this must also match
            // the Skia/AvaloniaView!!!
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
            var bounds = GetApplicationFrame();
            w = (int)bounds.Width;
            h = (int)bounds.Height;
        }

        public override IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            FixSize();

            var canvas = Surface.Canvas;
            canvas.RestoreToCount(0);
            canvas.Save();

            var screenScale = UIScreen.MainScreen.Scale;
            canvas.Scale((float)screenScale, (float)screenScale);

            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();

            return new WindowDrawingContextImpl(this);
        }

        public void Present()
        {
            _bitmap.LockPixels();
            IntPtr length;
            var pixels = _bitmap.GetPixels(out length);

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

            _bitmap.UnlockPixels();
        }
    }
}
