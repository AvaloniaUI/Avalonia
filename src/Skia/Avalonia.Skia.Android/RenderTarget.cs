using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Avalonia.Skia.Android;

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
        private readonly SurfaceView _surfaceView;
        private IntPtr _window;

        public WindowRenderTarget(SurfaceView surfaceView)
        {
            _surfaceView = surfaceView;
        }

        private void PrepareForDraw()
        {
            int width = _surfaceView.Width;
            var height = _surfaceView.Height;

            _window = NativeMethods.ANativeWindow_fromSurface(JNIEnv.Handle, _surfaceView.Holder.Surface.Handle);
            var buffer = new NativeMethods.ANativeWindow_Buffer();
            var rc = new NativeMethods.ARect() {right = width, bottom = height};
            NativeMethods.ANativeWindow_lock(_window, out buffer, ref rc);

            var colorType = buffer.format == NativeMethods.AndroidPixelFormat.WINDOW_FORMAT_RGB_565
                ? SKColorType.Rgb565 : SKImageInfo.PlatformColorType;

            var stride = buffer.stride * (colorType == SKColorType.Rgb565 ? 2 : 4);
            
            Surface = SKSurface.Create(buffer.width, buffer.height, colorType,
                SKAlphaType.Premul, buffer.bits, stride);
            
            if (Surface == null)
                throw new Exception("Unable to create Skia surface");
        }

        public override DrawingContext CreateDrawingContext()
        {
            PrepareForDraw();

            var canvas = Surface.Canvas;
            canvas.RestoreToCount(0);
            canvas.Save();
            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();

            return
                new DrawingContext(
                    new WindowDrawingContextImpl(this));
        }

        public void Present()
        {
            Surface?.Dispose();
            Surface = null;
            NativeMethods.ANativeWindow_unlockAndPost(_window);
            NativeMethods.ANativeWindow_release(_window);
            _window = IntPtr.Zero;
        }
    }
}
