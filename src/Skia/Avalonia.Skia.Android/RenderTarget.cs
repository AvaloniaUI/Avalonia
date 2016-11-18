using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using Android.Graphics;
using Android.Views;

namespace Avalonia.Skia
{
    internal partial class RenderTarget : IRenderTarget
    {
        public SKSurface Surface { get; protected set; }

        public virtual IDrawingContextImpl CreateDrawingContext()
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
        Bitmap _bitmap;
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

            _bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
            Surface = SKSurface.Create(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul, _bitmap.LockPixels(), width * 4);
        }

        private void GetPlatformWindowSize(out int w, out int h)
        {
            var surfaceView = _hwnd as SurfaceView;
            w = surfaceView.Width;
            h = surfaceView.Height;
        }

        public override IDrawingContextImpl CreateDrawingContext()
        {
            base.CreateDrawingContext();
            FixSize();

            var canvas = Surface.Canvas;
            canvas.RestoreToCount(0);
            canvas.Save();
            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();

            return new WindowDrawingContextImpl(this);
        }

        public void Present()
        {
            var surfaceView = _hwnd as SurfaceView;
            Canvas canvas = null;
            try
            {
                canvas = surfaceView.Holder.LockCanvas(null);
                _bitmap.UnlockPixels();
                canvas.DrawBitmap(_bitmap, 0, 0, null);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (canvas != null)
                    surfaceView.Holder.UnlockCanvasAndPost(canvas);
            }

            _bitmap.UnlockPixels();
        }
    }
}
