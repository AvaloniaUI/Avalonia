using System;
using System.Collections.Generic;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Views.InputMethods;
using Avalonia.Android.OpenGL;
using Avalonia.Android.OpenGL;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Controls;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering;

namespace Avalonia.Android
{
    public abstract class InvalidationAwareSurfaceView : SurfaceView, ISurfaceHolderCallback, IPlatformNativeSurfaceHandle
    {
        bool _invalidateQueued;
        readonly object _lock = new object();
        private readonly Handler _handler;

        IntPtr IPlatformHandle.Handle =>
            AndroidFramebuffer.ANativeWindow_fromSurface(JNIEnv.Handle, Holder.Surface.Handle);

        public InvalidationAwareSurfaceView(Context context) : base(context)
        {
            Holder.AddCallback(this);
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

        [Obsolete("deprecated")]
        public override void Invalidate(global::Android.Graphics.Rect dirty)
        {
            Invalidate();
        }

        [Obsolete("deprecated")]
        public override void Invalidate(int l, int t, int r, int b)
        {
            Invalidate();
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
