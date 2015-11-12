using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;
using Perspex.Skia.Android;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perspex.Android.Platform.SkiaPlatform
{
    public class WindowImpl : SkiaView, IAndroidView, IWindowImpl
    {
        protected AndroidKeyboardEventsHelper<WindowImpl> _keyboardHelper;

        private AndroidTouchEventsHelper<WindowImpl> _touchHelper;

        public WindowImpl(Context context) : base((Activity)context)
        {
            _keyboardHelper = new AndroidKeyboardEventsHelper<WindowImpl>(this);
            _touchHelper = new AndroidTouchEventsHelper<WindowImpl>(this, () => InputRoot, p => GetPerspexPointFromEvent(p));

            ClientSize = MaxClientSize;
            Resized = size => Invalidate(new Rect(size));
            Init();
        }

        public WindowImpl() : this(PerspexLocator.Current.GetService<IAndroidActivity>().Activity)
        {
        }

        protected virtual void Init()
        {
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

        public virtual Point GetPerspexPointFromEvent(MotionEvent e) => new Point(e.GetX(), e.GetY());

        public IInputRoot InputRoot { get; private set; }

        public Size ClientSize { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Size MaxClientSize => new Size(Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public View View => this;

        Action ITopLevelImpl.Activated { get; set; }

        IPlatformHandle ITopLevelImpl.Handle => this;

        public void Activate()
        {
        }

        public void Hide()
        {
            this.Visibility = ViewStates.Invisible;
        }

        public void Invalidate(Rect rect)
        {
            //not working if invalidate is called before Draw app is crashing !!!????
            //TODO: investigate and make better workaround also this solution is ok so far
            //if (this.Holder?.Surface != null)
            if(Holder?.Surface?.IsValid == true) base.Invalidate();
           // if (_drawInitialized) base.Invalidate();
        }

        public Point PointToScreen(Point point)
        {
            return point;
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            //still not implemented
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public void SetTitle(string title)
        {
        }

        public void Show()
        {
            this.Visibility = ViewStates.Visible;
        }

        public IDisposable ShowDialog()
        {
            throw new NotImplementedException();
        }

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

        private Queue<double> _avgDrawQueue = new Queue<double>();

        protected override void Draw()
        {
            _drawInitialized = true;

            DateTime begin = DateTime.Now;

            Paint?.Invoke(new Rect(new Point(0, 0), ClientSize));

            TimeSpan duration = DateTime.Now - begin;

            if (AndroidPlatform.Instance.DrawDebugInfo)
            {
                //draw some basic debug info about rendering
                //we can't create drawing context so push some info to std out
                //double ms = duration.TotalMilliseconds;
                //_avgDrawQueue.Enqueue(ms);
                //if (_avgDrawQueue.Count > 50) _avgDrawQueue.Dequeue();

                //double msAvg = _avgDrawQueue.Average();

                //string text = $"DrawType={AndroidPlatform.Instance.DefaultViewDrawType}, OnDraw={ms.ToString("0.00")}, aOnDraw={msAvg.ToString("0.00")}";
                //Log.Debug("render", text);
            }
        }

        private bool _drawInitialized = false;
    }
}