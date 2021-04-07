using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Accessibility;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.Specific;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Android
{
    public abstract class InvalidationAwareSurfaceView : SurfaceView, ISurfaceHolderCallback, IPlatformHandle
    {
        bool _invalidateQueued;
        private ISoftInputElement _softInputElement;
        readonly object _lock = new object();
        private readonly Handler _handler;

        public InvalidationAwareSurfaceView(Context context) : base(context)
        {
            Holder.AddCallback(this);
            _handler = new Handler(context.MainLooper);

            Focusable = true;
            FocusableInTouchMode = true;
        }

        public override void Invalidate()
        {
            lock (_lock)
            {
                if(_invalidateQueued)
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

        public override bool CheckInputConnectionProxy(View view)
        {
            return base.CheckInputConnectionProxy(view);
        }

        public override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo info)
        {
            base.OnInitializeAccessibilityNodeInfo(info);
        }

        public override bool OnCheckIsTextEditor()
        {
            return true;
        }
    }
}
