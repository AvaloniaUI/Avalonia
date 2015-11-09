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
    public abstract class SkiaView : SurfaceView, ISurfaceHolderCallback
    {
        private readonly Activity _context;
        bool _invalidateQueued;
        object _lock = new object();
        public SkiaView(Activity context) : base(context)
        {
            _context = context;
            SkiaPlatform.Initialize();
            Holder.AddCallback(this);
        }
        private IRenderTarget _renderTarget;

        public override void Invalidate()
        {
            lock (_lock)
            {
                if(_invalidateQueued)
                    return;
                _context.RunOnUiThread(() =>
                {
                    lock (_lock)
                    {
                        _invalidateQueued = false;
                    }
                    Draw();
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
            _renderTarget.Resize(width, height);
            Draw();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Log.Info("PERSPEX", "Surface Created");
            _renderTarget =
                PerspexLocator.Current.GetService<IPlatformRenderInterface>()
                    .CreateRenderer(new PlatformHandle(holder.Surface.Handle, "Surface"), Width, Height);
            Draw();
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Log.Info("PERSPEX", "Surface Destroyed");
            _renderTarget.Dispose();
            _renderTarget = null;
        }

        void Draw()
        {
            if(_renderTarget == null)
                return;
            using (var ctx = _renderTarget.CreateDrawingContext())
                OnRender(ctx);
        }

        protected abstract void OnRender(DrawingContext ctx);
    }
}