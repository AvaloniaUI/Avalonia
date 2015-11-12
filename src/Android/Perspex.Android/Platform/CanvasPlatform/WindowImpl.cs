using Android.Content;
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
    public class WindowImpl : View,
      //GridLayout, //may be GridLayout can be better if int he future we want to embed native views or controls
      IWindowImpl, IPlatformHandle, IAndroidCanvasView, IAndroidViewSupportRender, IDisposable
    {
        private IPointUnitService _pointService = PointUnitService.Instance;

        public ViewDrawType DrawType { get; }

        private AndroidViewRenderHelper<WindowImpl> _renderHelper;
        protected AndroidKeyboardEventsHelper<WindowImpl> _keyboardHelper;
        protected AndroidTouchEventsHelper<WindowImpl> _touchHelper;

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

        internal static bool _debug = false;

        public WindowImpl(Context context) : base(context)
        {
            DrawType = AndroidPlatform.Instance.DefaultViewDrawType == ViewDrawType.SurfaceViewCanvasOnDraw ? ViewDrawType.CanvasOnDraw : AndroidPlatform.Instance.DefaultViewDrawType;

            _keyboardHelper = new AndroidKeyboardEventsHelper<WindowImpl>(this);
            _touchHelper = new AndroidTouchEventsHelper<WindowImpl>(this, () => InputRoot, GetPerspexPointFromEvent);
            _renderHelper = AndroidViewRenderHelper<WindowImpl>.Create(this, DrawType);

            //LayerType = LayerType.Hardware;
            //SetLayerType(LayerType.Hardware, null);

            ClientSize = MaxClientSize;
            Resized = size => Invalidate(new Rect(size));
            this.Init();
        }

        public WindowImpl() : this(PerspexLocator.Current.GetService<IAndroidActivity>().Activity)
        {
        }

        protected virtual void Init()
        {
            HandleEvents = false;

            //if the background is not transparent nothing is visible!!!
            //this is the case when inherit from GridLayout, not from View
            //this.Background = new AG.Drawables.ColorDrawable(AG.Color.Transparent);
        }

        public Rect Bounds { get; set; }

        public IInputRoot InputRoot { get; private set; }
        IntPtr IPlatformHandle.Handle => Handle;
        string IPlatformHandle.HandleDescriptor => "Perspex View";

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
            this.Visibility = ViewStates.Invisible;
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

        public AG.Canvas Canvas { get; private set; }

        public Dictionary<object, IDisposable> VisualCaches
        {
            get
            {
                return _renderHelper.VisualCaches;
            }
        }

        protected TimeSpan _onDrawDuration;

        private DateTime _beginDraw;

        public virtual void PreRender(AG.Canvas canvas, Rect rect)
        {
            _beginDraw = DateTime.Now;
        }

        public virtual void Render(AG.Canvas canvas, Rect rect)
        {
            Canvas = canvas;
            Paint?.Invoke(rect);
        }

        public virtual void PostRender(AG.Canvas canvas, Rect rect)
        {
            _renderHelper.DrawDebugInfoToCanvas(canvas);
            _touchHelper.DrawLastMousePoint(canvas);
        }

        protected override void OnDraw(AG.Canvas canvas)
        {
            _renderHelper.OnDraw(canvas);
        }

        void IDisposable.Dispose()
        {
            _renderHelper.Dispose();
            _touchHelper.Dispose();
            _keyboardHelper.Dispose();
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            Resize(MaxClientSize.Width, MaxClientSize.Height);
        }
    }
}