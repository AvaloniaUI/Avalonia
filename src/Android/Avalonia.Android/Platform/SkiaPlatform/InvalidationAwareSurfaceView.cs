using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Platform;

namespace Avalonia.Android
{
    internal abstract class InvalidationAwareSurfaceView : SurfaceView, ISurfaceHolderCallback, INativePlatformHandleSurface
    {
        bool _invalidateQueued;
        readonly object _lock = new object();
        private readonly Handler _handler;

        IntPtr IPlatformHandle.Handle =>
            AndroidFramebuffer.ANativeWindow_fromSurface(JNIEnv.Handle, Holder.Surface.Handle);

        public InvalidationAwareSurfaceView(Context context) : base(context)
        {
            Holder.AddCallback(this);
            Holder.SetFormat(global::Android.Graphics.Format.Transparent);
            _handler = new Handler(context.MainLooper);
        }

        public override void Invalidate()
        {
            lock (_lock)
            {
                if (_invalidateQueued)
                    return;
                _handler.Post(() =>
                {
                    if (Holder.Surface?.IsValid != true)
                        return;
                    try
                    {
                        DoDraw();
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "Avalonia", e.ToString());
                    }
                });
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
            Log.Info("AVALONIA", "Surface Changed");
            DoDraw();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Log.Info("AVALONIA", "Surface Created");
            DoDraw();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Log.Info("AVALONIA", "Surface Destroyed");

        }

        protected void DoDraw()
        {
            lock (_lock)
            {
                _invalidateQueued = false;
            }
            Draw();
        }
        protected abstract void Draw();
        public string HandleDescriptor => "SurfaceView";

        public PixelSize Size => new PixelSize(Holder.SurfaceFrame.Width(), Holder.SurfaceFrame.Height());

        public double Scaling => Resources.DisplayMetrics.Density;
    }
}
