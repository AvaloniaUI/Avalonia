using System;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Logging;
using Avalonia.Platform;
using Java.Lang;

namespace Avalonia.Android
{
    internal abstract class InvalidationAwareSurfaceView : SurfaceView, ISurfaceHolderCallback2, INativePlatformHandleSurface
    {
        private IntPtr _nativeWindowHandle = IntPtr.Zero;
        private PixelSize _size = new(1, 1);
        private double _scaling = 1;

        public event EventHandler? SurfaceWindowCreated;
        public PixelSize Size => _size;
        public double Scaling => _scaling;

        IntPtr IPlatformHandle.Handle => _nativeWindowHandle;
        string IPlatformHandle.HandleDescriptor => "SurfaceView";

        protected InvalidationAwareSurfaceView(Context context) : base(context)
        {
            if (Holder is null)
                throw new InvalidOperationException(
                    "SurfaceView.Holder was not expected to be null during InvalidationAwareSurfaceView initialization.");

            Holder.AddCallback(this);
            Holder.SetFormat(global::Android.Graphics.Format.Transparent);
        }

        protected override void Dispose(bool disposing)
        {
            ReleaseNativeWindowHandle();
            base.Dispose(disposing);
        }

        public virtual void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, $"InvalidationAwareSurfaceView Changed. Format:{format} Size:{width} x {height}");
            CacheSurfaceProperties(holder);
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView Created");
            CacheSurfaceProperties(holder);
            SurfaceWindowCreated?.Invoke(this, EventArgs.Empty);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView Destroyed");
            ReleaseNativeWindowHandle();
            _size = new PixelSize(1, 1);
            _scaling = 1;
        }

        public virtual void SurfaceRedrawNeeded(ISurfaceHolder holder)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView RedrawNeeded");
        }

        public virtual void SurfaceRedrawNeededAsync(ISurfaceHolder holder, IRunnable drawingFinished)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView RedrawNeededAsync");
        }

        private void CacheSurfaceProperties(ISurfaceHolder holder)
        {
            var surface = holder?.Surface;
            var newHandle = IntPtr.Zero;
            if (surface?.Handle is { } handle)
            {
                newHandle = AndroidFramebuffer.ANativeWindow_fromSurface(JNIEnv.Handle, handle);
            }

            if (Interlocked.Exchange(ref _nativeWindowHandle, newHandle) is var oldHandle
                && oldHandle != IntPtr.Zero)
            {
                AndroidFramebuffer.ANativeWindow_release(oldHandle);
            }

            var frame = holder?.SurfaceFrame;
            _size = frame != null ? new PixelSize(frame.Width(), frame.Height()) : new PixelSize(1, 1);
            _scaling = Resources?.DisplayMetrics?.Density ?? 1;
        }

        private void ReleaseNativeWindowHandle()
        {
            if (Interlocked.Exchange(ref _nativeWindowHandle, IntPtr.Zero) is var oldHandle
                && oldHandle != IntPtr.Zero)
            {
                AndroidFramebuffer.ANativeWindow_release(oldHandle);
            }
        }
    }
}
