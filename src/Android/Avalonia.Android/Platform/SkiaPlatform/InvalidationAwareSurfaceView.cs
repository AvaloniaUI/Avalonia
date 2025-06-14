using System;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.Android
{
    internal abstract class InvalidationAwareSurfaceView : SurfaceView, ISurfaceHolderCallback, INativePlatformHandleSurface
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
            CacheSurfaceProperties(holder);
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView Changed");
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            CacheSurfaceProperties(holder);
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView Created");
            SurfaceWindowCreated?.Invoke(this, EventArgs.Empty);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            ReleaseNativeWindowHandle();
            _size = new PixelSize(1, 1);
            _scaling = 1;
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                .Log(this, "InvalidationAwareSurfaceView Destroyed");
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
