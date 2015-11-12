using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Perspex.Android.Platform.CanvasPlatform.Specific.Helpers;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;
using System;
using System.Collections.Generic;

using AG = Android.Graphics;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public class SurfaceWindowImpl : SurfaceView, IWindowImpl, IPlatformHandle, IAndroidCanvasView, IAndroidViewSupportRender
    {
        public SurfaceWindowImpl(Context context) : base(context)
        {
            DrawType = ViewDrawType.SurfaceViewCanvasOnDraw;

            _keyboardHelper = new AndroidKeyboardEventsHelper<SurfaceWindowImpl>(this);
            _touchHelper = new AndroidTouchEventsHelper<SurfaceWindowImpl>(this, () => InputRoot, GetPerspexPointFromEvent);

            ClientSize = MaxClientSize;
            Resized = size => Invalidate(new Rect(size));
            Init();
            Background = new AG.Drawables.ColorDrawable(AG.Color.Transparent);
        }

        public SurfaceWindowImpl() : this(PerspexLocator.Current.GetService<IAndroidActivity>().Activity)
        {
        }

        public void SurfaceRedrawNeeded(ISurfaceHolder holder)
        {
            Log.Debug("SurfaceView", "SurfaceRedrawNeeded...");
            //TryDraw(holder);
        }

        protected override void OnDraw(Canvas canvas)
        {
            //base.OnDraw(canvas);
            this._renderHelper.OnDraw(canvas);
        }

        private IPointUnitService _pointService = PointUnitService.Instance;

        public ViewDrawType DrawType { get; }

        private SurfaceViewRenderHelper<SurfaceWindowImpl> _renderHelper;
        protected AndroidKeyboardEventsHelper<SurfaceWindowImpl> _keyboardHelper;
        protected AndroidTouchEventsHelper<SurfaceWindowImpl> _touchHelper;

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

        protected virtual void Init()
        {
            //SetLayerType(LayerType.Hardware, null);
            _renderHelper = SurfaceViewRenderHelper<SurfaceWindowImpl>.Create(this, DrawType);

            HandleEvents = false;

            //if the background is not transparent nothing is visible!!!
            //this is the case when inherit from GridLayout, not from View
            //this.Background = new AG.Drawables.ColorDrawable(AG.Color.Transparent);
        }

        public Rect Bounds { get; set; }

        public IInputRoot InputRoot { get; private set; }
        IntPtr IPlatformHandle.Handle => Handle;
        string IPlatformHandle.HandleDescriptor => "Perspex SurfaceView";

        public void Resize(double width, double height)
        {
            if (ClientSize == new Size(width, height)) return;
            ClientSize = new Size(width, height);
            Resized(ClientSize);
        }

        public Size ClientSize { get; set; }
        IPlatformHandle ITopLevelImpl.Handle => this;
        Action ITopLevelImpl.Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }

        public void Activate()
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public Point PointToScreen(Point point)
        {
            return point;
        }

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public Size MaxClientSize => _pointService.NativeToPerspex(new Size(Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels));

        public View View => this;

        public void SetTitle(string title)
        {
        }

        void ITopLevelImpl.Show()
        {
            this.Visibility = ViewStates.Visible;
        }

        public IDisposable ShowDialog()
        {
            throw new NotImplementedException();
        }

        public void Hide()
        {
        }

        public virtual Point GetPerspexPointFromEvent(MotionEvent e) => _pointService.NativeToPerspex(new Point(e.GetX(), e.GetY()));

        public override bool DispatchTouchEvent(MotionEvent e)
        {
            bool callBase;
            bool? result = _touchHelper.DispatchTouchEvent(e, out callBase);
            bool baseResult = callBase ? base.DispatchTouchEvent(e) : false;

            return result != null ? result.Value : baseResult;
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            bool callBase;
            bool? res = _keyboardHelper.DispatchKeyEvent(e, out callBase);
            bool baseResult = callBase ? base.DispatchKeyEvent(e) : false;

            return res != null ? res.Value : baseResult;
        }

        public void Invalidate(Rect rect)
        {
            this._renderHelper.Invalidate(rect);
        }

        public virtual void PreRender(Canvas canvas, Rect rect)
        {
        }

        public virtual void Render(Canvas canvas, Rect rect)
        {
            Canvas = canvas;
            Paint?.Invoke(rect);
        }

        public virtual void PostRender(Canvas canvas, Rect rect)
        {
            _renderHelper.DrawDebugInfoToCanvas(canvas);
            _touchHelper.DrawLastMousePoint(canvas);
        }

        public AG.Canvas Canvas { get; private set; }

        public Dictionary<object, IDisposable> VisualCaches => _renderHelper.VisualCaches;

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            Resize(MaxClientSize.Width, MaxClientSize.Height);
        }
    }
}