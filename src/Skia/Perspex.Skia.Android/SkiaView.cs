using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia.Android
{
    public abstract class SkiaView : SurfaceView, ISurfaceHolderCallback, IPlatformHandle
    {
        private readonly Activity _context;
        bool _invalidateQueued;
        readonly object _lock = new object();
        private readonly Handler _handler;

        public SkiaView(Activity context) : base(context)
        {
            _context = context;
            SkiaPlatform.Initialize();
            Holder.AddCallback(this);
            _handler = new Handler(context.MainLooper);
        }

        public override void Invalidate()
        {
            lock (_lock)
            {
                if(_invalidateQueued)
                    return;
                _handler.Post(() =>
                {
                    lock (_lock)
                    {
                        _invalidateQueued = false;
                    }
                    try
                    {
                        Draw();
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(LogPriority.Error, "Perspex", e.ToString());
                    }
                });
            }
        }

        public override void Invalidate(global::Android.Graphics.Rect dirty)
        {
            Invalidate();
        }

        public override void Invalidate(int l, int t, int r, int b)
        {
            Invalidate();
        }

        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
            Log.Info("PERSPEX", "Surface Changed");
            Draw();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Log.Info("PERSPEX", "Surface Created");
            Draw();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Log.Info("PERSPEX", "Surface Destroyed");
        }

        protected abstract void Draw();
        public string HandleDescriptor => "SurfaceView";
    }
}