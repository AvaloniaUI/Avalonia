using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;

using Avalonia.Android.OpenGL;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class TopLevelImpl : IAndroidView, ITopLevelImpl, EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo
    {
        private readonly IGlPlatformSurface _gl;
        private readonly IFramebufferPlatformSurface _framebuffer;

        private readonly AndroidKeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private readonly AndroidTouchEventsHelper<TopLevelImpl> _touchHelper;

        private ViewImpl _view;

        public TopLevelImpl(Context context, bool placeOnTop = false)
        {
            _view = new ViewImpl(context, this, placeOnTop);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _touchHelper = new AndroidTouchEventsHelper<TopLevelImpl>(this, () => InputRoot,
                p => GetAvaloniaPointFromEvent(p));

            _gl = GlPlatformSurface.TryCreate(this);
            _framebuffer = new FramebufferManager(this);

            MaxClientSize = new Size(_view.Resources.DisplayMetrics.WidthPixels,
                _view.Resources.DisplayMetrics.HeightPixels);
        }



        private bool _handleEvents;

        public bool HandleEvents
        {
            get { return _handleEvents; }
            set
            {
                _handleEvents = value;
                _keyboardHelper.HandleEvents = _handleEvents;
            }
        }

        public virtual Point GetAvaloniaPointFromEvent(MotionEvent e) => new Point(e.GetX(), e.GetY());

        public IInputRoot InputRoot { get; private set; }

        public virtual Size ClientSize
        {
            get
            {
                if (_view == null)
                    return new Size(0, 0);
                return new Size(_view.Width, _view.Height);
            }
            set
            {

            }
        }

        public IMouseDevice MouseDevice => AndroidMouseDevice.Instance;

        public Action Closed { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Size MaxClientSize { get; protected set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public View View => _view;

        internal InvalidationAwareSurfaceView InternalView => _view;

        public IPlatformHandle Handle => _view;

        public IEnumerable<object> Surfaces => new object[] { _gl, _framebuffer };

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            return new ImmediateRenderer(root);
        }

        public virtual void Hide()
        {
            _view.Visibility = ViewStates.Invisible;
        }

        public void Invalidate(Rect rect)
        {
            if (_view.Holder?.Surface?.IsValid == true) _view.Invalidate();
        }

        public Point PointToClient(PixelPoint point)
        {
            return point.ToPoint(1);
        }

        public PixelPoint PointToScreen(Point point)
        {
            return PixelPoint.FromPoint(point, 1);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            //still not implemented
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }
        
        public virtual void Show()
        {
            _view.Visibility = ViewStates.Visible;
        }

        public double RenderScaling => 1;

        void Draw()
        {
            Paint?.Invoke(new Rect(new Point(0, 0), ClientSize));
        }
        
        public virtual void Dispose()
        {
            _view.Dispose();
            _view = null;
        }

        protected virtual void OnResized(Size size)
        {
            Resized?.Invoke(size);
        }

        class ViewImpl : InvalidationAwareSurfaceView, ISurfaceHolderCallback
        {
            private readonly TopLevelImpl _tl;
            private Size _oldSize;
            public ViewImpl(Context context,  TopLevelImpl tl, bool placeOnTop) : base(context)
            {
                _tl = tl;
                if (placeOnTop)
                    SetZOrderOnTop(true);
            }

            protected override void Draw()
            {
                _tl.Draw();
            }

            public override bool DispatchTouchEvent(MotionEvent e)
            {
                bool callBase;
                bool? result = _tl._touchHelper.DispatchTouchEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchTouchEvent(e) : false;

                return result != null ? result.Value : baseResult;
            }

            public override bool DispatchKeyEvent(KeyEvent e)
            {
                bool callBase;
                bool? res = _tl._keyboardHelper.DispatchKeyEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchKeyEvent(e) : false;

                return res != null ? res.Value : baseResult;
            }


            void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                var newSize = new Size(width, height);

                if (newSize != _oldSize)
                {
                    _oldSize = newSize;
                    _tl.OnResized(newSize);
                }

                base.SurfaceChanged(holder, format, width, height);
            }
        }

        public IPopupImpl CreatePopup() => null;
        
        public Action LostFocus { get; set; }
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        IntPtr EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo.Handle =>
            AndroidFramebuffer.ANativeWindow_fromSurface(JNIEnv.Handle, _view.Holder.Surface.Handle);

        public PixelSize Size => new PixelSize(_view.Holder.SurfaceFrame.Width(), _view.Holder.SurfaceFrame.Height());

        public double Scaling => RenderScaling;

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            throw new NotImplementedException();
        }
    }
}
