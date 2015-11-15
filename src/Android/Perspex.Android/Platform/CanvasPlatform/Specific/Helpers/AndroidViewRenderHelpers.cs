using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Perspex.Android.CanvasRendering;
using Perspex.Android.Platform.Specific.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using P = Perspex.Platform;

namespace Perspex.Android.Platform.CanvasPlatform.Specific.Helpers
{
    public abstract class AndroidViewRenderHelper<TView> : Java.Lang.Object, IDisposable where TView : View, P.IWindowImpl, IAndroidViewSupportRender
    {
        private static bool _debug = false;

        protected AndroidViewRenderHelper(TView view)
        {
            this.View = view;
            VisualCaches = null;
        }

        public Dictionary<object, IDisposable> VisualCaches { get; protected set; }

        public abstract void OnDraw(Canvas canvas);

        public abstract void Invalidate(Rect rect);

        public TView View { get; private set; }

        public ViewDrawType DrawType { get; private set; }

        public static AndroidViewRenderHelper<TView> Create(TView view, ViewDrawType drawType)
        {
            AndroidViewRenderHelper<TView> result;
            switch (drawType)
            {
                case ViewDrawType.BitmapBackgroundRender:
                    result = new BitmapBackgroundRenderHelper<TView>(view);
                    break;

                case ViewDrawType.BitmapOnPreDraw:
                    result = new BitmapPreDrawRenderHelper<TView>(view);
                    break;

                case ViewDrawType.CanvasOnDraw:
                    result = new CanvasOnDrawAndroidViewRenderHelper<TView>(view);
                    break;

                default:
                    throw new NotSupportedException();
            }

            Init(result, drawType);

            return result;
        }

        protected static void Init(AndroidViewRenderHelper<TView> helper, ViewDrawType drawType)
        {
            helper.DrawType = drawType;
            helper.SetDebugInfo("DrawType", drawType.ToString());
        }

        public virtual new void Dispose()
        {
            View = null;
            _debugInfoPaint?.Dispose();
        }

        public Dictionary<string, string> DebugInfo { get; } = new Dictionary<string, string>();

        protected void SetDebugInfo(string key, string value) => DebugInfo[key] = value;

        private const int _avgValuesForStat = 50;

        private Dictionary<string, Queue<double>> _renderStats = new Dictionary<string, Queue<double>>();

        protected void UpdateRenderStat(string key, double ms)
        {
            Queue<double> statsQueue;
            if (!_renderStats.TryGetValue(key, out statsQueue))
            {
                statsQueue = new Queue<double>();
                _renderStats[key] = statsQueue;
            }

            statsQueue.Enqueue(ms);
            if (statsQueue.Count > _avgValuesForStat) statsQueue.Dequeue();

            SetDebugInfo(key, ms.ToString("0.0"));
            SetDebugInfo($"a{key}", (statsQueue.Average()).ToString("0.0"));
        }

        protected void RenderView(Canvas canvas, Rect rect)
        {
            DateTime begin = DateTime.Now;

            View.PreRender(canvas, rect);
            View.Render(canvas, rect);
            View.PostRender(canvas, rect);

            TimeSpan duration = DateTime.Now - begin;

            if (_debug) Log.Debug("AndroidViewRenderHelper", $"RenderView {duration}");

            UpdateRenderStat("RenderView", duration.TotalMilliseconds);
            UpdateRenderStat("mfps", 1000 / duration.TotalMilliseconds);
        }

        private Paint _debugInfoPaint = null;

        public void DrawDebugInfoToCanvas(Canvas canvas)
        {
            if (!AndroidPlatform.Instance.DrawDebugInfo) return;

            if (DebugInfo.Count == 0) return;

            if (_debugInfoPaint == null) _debugInfoPaint = new TextPaint(PaintFlags.AntiAlias) { Color = Color.Red, TextAlign = Paint.Align.Left, TextSize = 20 };

            var sb = new StringBuilder();
            foreach (var o in DebugInfo)
            {
                sb.Append($"{o.Key} = {o.Value}; ");
            }
            string sdb = sb.ToString();
            var r = new global::Android.Graphics.Rect();
            _debugInfoPaint.GetTextBounds(sdb, 0, sdb.Length, r);

            canvas.DrawText(sdb, View.Width - r.Right, View.Height - r.Bottom, _debugInfoPaint);

            if (_debug) Log.Debug("AndroidViewRenderHelper", sdb);
        }
    }

    public class CanvasOnDrawAndroidViewRenderHelper<TView> : AndroidViewRenderHelper<TView> where TView : View, P.IWindowImpl, IAndroidViewSupportRender
    {
        private Rect _rect;

        public CanvasOnDrawAndroidViewRenderHelper(TView view) : base(view)
        {
        }

        public override void Invalidate(Rect rect)
        {
            _rect = rect;
            View.Invalidate(rect.ToAndroidGraphics());
        }

        public override void OnDraw(Canvas canvas)
        {
            RenderView(canvas, _rect);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    public class BitmapBackgroundRenderHelper<TView> : AndroidViewRenderHelper<TView> where TView : View, P.IWindowImpl, IAndroidViewSupportRender
    {
        private bool _debug = WindowImpl._debug;
        private Bitmap _bitmap;
        private Rect _rect;
        private Paint _paint = new Paint();
        private ConcurrentStack<Rect> _invalidateRects = new ConcurrentStack<Rect>();
        private object _invalidateRectsSync = new object();
        private object _drawSync = new object();
        private Stack<Bitmap> _renderingBitmapsCache = new Stack<Bitmap>();
        private Dictionary<Bitmap, Canvas> _bmCanvases = new Dictionary<Bitmap, Canvas>();
        private CancellationTokenSource _cancelationTokenSource;

        public BitmapBackgroundRenderHelper(TView view) : base(view)
        {
            _cancelationTokenSource = new CancellationTokenSource();
            VisualCaches = new Dictionary<object, IDisposable>();
            Task.Factory.StartNew(RenderingLoop, _cancelationTokenSource.Token, _cancelationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public override void Invalidate(Rect rect)
        {
            _rect = rect;
            _invalidateRects.Push(rect);
            lock (_invalidateRectsSync)
            {
                Monitor.Pulse(_invalidateRectsSync);
            }
        }

        public override void OnDraw(Canvas canvas)
        {
            var begin = DateTime.Now;
            lock (_drawSync)
            {
                if (_bitmap != null)
                {
                    canvas.DrawBitmap(_bitmap, 0, 0, _paint);
                }
            }

            UpdateRenderStat("OnDraw", (DateTime.Now - begin).TotalMilliseconds);
        }

        private void RenderingLoop(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            if (_debug) Log.Debug("render", "RenderingLoop Start ...");

            AndroidPlatform.Instance.RegisterRenderingTaskId(Task.CurrentId.Value);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Rect rect = default(Rect);
                    bool hasRect = false;

                    hasRect = _invalidateRects.Count > 0; ;
                    if (!hasRect)
                    {
                        lock (_invalidateRectsSync)
                        {
                            hasRect = Monitor.Wait(_invalidateRectsSync, 1000);
                        }
                    }

                    if (!hasRect)
                    {
                        DisposeVisualCaches();
                        continue;
                    }

                    if (_invalidateRects.Count < 1) continue;
                    int rCnt = _invalidateRects.Count;

                    Rect[] rects = new Rect[rCnt > 1 ? (rCnt - 1) : 1];
                    int cnt = _invalidateRects.TryPopRange(rects, 0, rects.Length);
                    if (_debug) Log.Debug("render", $"RenderingLoop processing {cnt} rects");
                    if (cnt == 1)
                    {
                        rect = rects[0];
                        //assuming  this is the last frame so we dispose the caches
                        DisposeVisualCaches();
                    }
                    else if (cnt > 1)
                    {
                        //for now all the rects are the same but may be in the future this can change
                        var minX = rects.Min(r => r.X);
                        var minY = rects.Min(r => r.Y);
                        var maxRight = rects.Max(r => r.Right);
                        var maxBottom = rects.Max(r => r.Bottom);
                        rect = new Rect(minX, minY, maxRight - minX, maxBottom - minY);
                    }
                    else
                    {
                        //Should never happend
                        hasRect = false;
                    }

                    if (hasRect)
                    {
                        Render(rect);
                    }
                }
                catch (Exception e)
                {
                    Log.Debug("render", "RenderingLoop Exception {0} ...", e);
                }
            }
            AndroidPlatform.Instance.UnRegisterRenderingTaskId(Task.CurrentId.Value);
            Log.Debug("render", "RenderingLoop End ...");
        }

        private void Render(Rect rect)
        {
            DateTime begin = DateTime.Now;
            if (_debug) Log.Debug("render", $"RenderingLoop new invalidation ... {rect} {begin}");
            Bitmap bitmap;

            int w = View.Width;
            int h = View.Height;

            if (w == 0)
            {
                var s = PointUnitService.Instance.PerspexToNative(View.ClientSize);
                w = (int)s.Width;
                h = (int)s.Height;
            }

            if (_renderingBitmapsCache.Count > 0)
            {
                // _renderingBitmapsCache.TryPop(out bitmap);
                bitmap = _renderingBitmapsCache.Pop();
            }
            else
            {
                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            }

            if (bitmap.Width != w || bitmap.Height != h)
            {
                bitmap.Dispose();
                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
                foreach (var c in _bmCanvases)
                {
                    c.Value.Dispose();
                }
                _bmCanvases.Clear();
            }

            Canvas canvas;
            if (!_bmCanvases.TryGetValue(bitmap, out canvas))
            {
                canvas = new Canvas(bitmap);
                _bmCanvases.Add(bitmap, canvas);
            }

            RenderView(canvas, _rect);

            //???no need dispose canvas is cached
            //canvas.Dispose();
            lock (_drawSync)
            {
                if (_bitmap != null) _renderingBitmapsCache.Push(_bitmap);
                _bitmap = bitmap;
            }
            var nr = rect.ToAndroidGraphics();

            View.PostInvalidate(nr.Left, nr.Top, nr.Right, nr.Bottom);
            //View.PostInvalidateDelayed(1, nr.Left, nr.Top, nr.Right, nr.Bottom);
        }

        private void DisposeVisualCaches()
        {
            lock (_drawSync)
            {
                if (_debug) Log.Debug("render", $"RenderingLoop disposing {VisualCaches.Count} visual caches");
                foreach (var vc in VisualCaches)
                {
                    vc.Value.Dispose();
                }

                VisualCaches.Clear();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _cancelationTokenSource.Cancel();
            _cancelationTokenSource = null;
        }
    }

    public class BitmapPreDrawRenderHelper<TView> : AndroidViewRenderHelper<TView> where TView : View, P.IWindowImpl, IAndroidViewSupportRender
    {
        private class OnPreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
        {
            private Func<bool> _preDraw;

            public OnPreDrawListener(Func<bool> preDraw)
            {
                _preDraw = preDraw;
            }

            public bool OnPreDraw()
            {
                return _preDraw();
            }
        }

        private Bitmap _bitmap;
        private Rect _rect;
        private Paint _paint = new Paint();
        private OnPreDrawListener _listener;

        public BitmapPreDrawRenderHelper(TView view) : base(view)
        {
            _listener = new OnPreDrawListener(OnPreDraw);
            View.ViewTreeObserver.AddOnPreDrawListener(_listener);
        }

        public override void Dispose()
        {
            base.Dispose();
            View.ViewTreeObserver.RemoveOnPreDrawListener(_listener);
            _listener.Dispose();
            _bitmap.Dispose();
            _paint.Dispose();
        }

        public override void Invalidate(Rect rect)
        {
            _rect = rect;
            View.Invalidate(rect.ToAndroidGraphics());
        }

        public override void OnDraw(Canvas canvas)
        {
            var begin = DateTime.Now;

            if (_bitmap != null)
            {
                canvas.DrawBitmap(_bitmap, 0, 0, _paint);
            }

            UpdateRenderStat("OnDraw", (DateTime.Now - begin).TotalMilliseconds);
        }

        public bool OnPreDraw()
        {
            int w = View.Width;
            int h = View.Height;
            Bitmap bitmap = _bitmap;
            if (bitmap == null)
            {
                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            }
            else if (bitmap.Width != w || bitmap.Height != h)
            {
                bitmap.Dispose();
                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            }
            using (var canvas = new Canvas(bitmap))
            {
                RenderView(canvas, _rect);
            }
            _bitmap = bitmap;
            return true;
        }
    }

    public class SurfaceViewRenderHelper<TView> : AndroidViewRenderHelper<TView> where TView : SurfaceView, P.IWindowImpl, IAndroidViewSupportRender
    {
        private class SurfaceHolderCallback : Java.Lang.Object, ISurfaceHolderCallback//, ISurfaceHolderCallback2
        {
            private SurfaceViewRenderHelper<TView> _parent;

            public SurfaceHolderCallback(SurfaceViewRenderHelper<TView> parent)
            {
                _parent = parent;
            }

            public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
            {
                _parent.SurfaceChanged(holder, format, width, height);
            }

            public void SurfaceCreated(ISurfaceHolder holder)
            {
                _parent.SurfaceCreated(holder);
            }

            public void SurfaceDestroyed(ISurfaceHolder holder)
            {
                _parent.SurfaceDestroyed(holder);
            }

            public void SurfaceRedrawNeeded(ISurfaceHolder holder)
            {
                _parent.SurfaceRedrawNeeded(holder);
            }
        }

        public int DelayBetweenDrawms { get; set; }
        protected ISurfaceHolder _holder;
        private Task _task;
        private CancellationTokenSource _cancelationTokenSource;
        private CancellationToken _cancelationToken;

        private ConcurrentStack<Rect> _pendingInvalidations = new ConcurrentStack<Rect>();
        private object _invalidationRectsSync = new object();

        private Format _surfaceFormat = Format.Rgba8888;
        private Bitmap.Config _bitmapFormat = Bitmap.Config.Argb8888;

        public static new SurfaceViewRenderHelper<TView> Create(TView view, ViewDrawType drawType)
        {
            SurfaceViewRenderHelper<TView> result;

            switch (drawType)
            {
                case ViewDrawType.SurfaceViewCanvasOnDraw:
                    result = new SurfaceViewRenderHelper<TView>(view);
                    break;

                default: throw new NotSupportedException();
            }

            Init(result, drawType);

            return result;
        }

        public SurfaceViewRenderHelper(TView view) : base(view)
        {
            //max 16fps -60ms, 25fps - 40ms, max 30fps 30ms, max 60 fps-17ms
            DelayBetweenDrawms = 40;
            view.Holder.AddCallback(new SurfaceHolderCallback(this));
        }

        public override void Invalidate(Rect rect)
        {
            lock (_invalidationRectsSync)
            {
                _pendingInvalidations.Push(rect);
                Monitor.Pulse(_invalidationRectsSync);
            }
        }

        public override void OnDraw(Canvas canvas)
        {
            //do nothing here we are drawing on another thread
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            Log.Debug("SurfaceViewRenderAdapter", $"SurfaceChanged format={format} width ={width} height={height}...");

            var ps = PointUnitService.Instance;
            Size newSize = new Size(ps.NativeToPerspexX(width), ps.NativeToPerspexY(height));
            if (View.ClientSize != newSize)
            {
                View.ClientSize = newSize;
                View.Resized?.Invoke(View.ClientSize);
            }

            ScheduleRender();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Log.Debug("SurfaceViewRenderAdapter", "SurfaceCreated ...");

            _holder = holder;
            _holder.SetFormat(_surfaceFormat);

            if (_task != null)
            {
                StopTask();
            }

            if (_task == null) StartTask();

            ScheduleRender();
        }

        private void SurfaceRedrawNeeded(ISurfaceHolder holder)
        {
            Log.Debug("SurfaceViewRenderAdapter", "SurfaceRedrawNeeded ...");
            ScheduleRender();
        }

        private void ScheduleRender()
        {
            if (_pendingInvalidations.Count > 0)
            {
                Invalidate(new Rect(0, 0, View.ClientSize.Width, View.ClientSize.Height));
            }
        }

        private void RenderingLoop(object obj)
        {
            AndroidPlatform.Instance.RegisterRenderingTaskId(Task.CurrentId.Value);
            Log.Debug("SurfaceViewRenderAdapter", $"DrawingTask Enter Id={Task.CurrentId.Value}... ");

            //sometime the ui is not ready for other thread rendering
            Thread.Sleep(10 * DelayBetweenDrawms);

            var cancelationToken = (CancellationToken)obj;
            while (!cancelationToken.IsCancellationRequested)
            {
                try
                {
                    bool waitResult = false;
                    //wait few miliseconds so we are not heating the processor too much
                    Thread.Sleep(DelayBetweenDrawms);

                    if (_pendingInvalidations.Count < 1)
                    {
                        lock (_invalidationRectsSync)
                        {
                            waitResult = Monitor.Wait(_invalidationRectsSync, 1000);
                            Thread.Sleep(0);
                        }
                    }

                    if (cancelationToken.IsCancellationRequested) break;

                    //if invalidations are more than one render one time after this
                    //so we are sure we have correct state
                    int requestCnt = _pendingInvalidations.Count > 1 ? _pendingInvalidations.Count - 1 : _pendingInvalidations.Count;

                    if (requestCnt < 1)
                    {
                        //we can not continue with drawing
                        continue;
                    }

                    var rects = new Rect[requestCnt];
                    int rectsCount = _pendingInvalidations.TryPopRange(rects);
                    if (rectsCount < 1)
                    {
                        //should never happen but who knows
                        continue;
                    }

                    Rect rect;
                    if (rectsCount == 1)
                    {
                        rect = rects[0];
                    }
                    else
                    {
                        double minX = rects.Min(r => r.X);
                        double minY = rects.Min(r => r.Y);
                        double maxRight = rects.Max(r => r.Right);
                        double maxBottom = rects.Max(r => r.Bottom);
                        rect = new Rect(minX, minY, maxRight - minX, maxBottom - minY);
                    }

                    RenderRect(rect);
                }
                catch (Exception e)
                {
                    Log.Debug("SurfaceViewRenderAdapter", $"Exception in DrawingTask {e}");
                }
            }
            Log.Debug("SurfaceViewRenderAdapter", $"DrawingTask Exit Id={Task.CurrentId.Value}...");
        }

        private void RenderRect(Rect rect)
        {
            //Log.Debug("SurfaceViewRenderAdapter", $"RenderRect Enter rect={rect}... ");
            var surfaceCanvas = _holder.LockCanvas();

            try
            {
                lock (_holder)
                {
                    RenderView(surfaceCanvas, rect);
                }
            }
            finally
            {
                _holder.UnlockCanvasAndPost(surfaceCanvas);
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            StopTask();
        }

        private void StartTask()
        {
            _cancelationTokenSource = new CancellationTokenSource();
            _cancelationToken = _cancelationTokenSource.Token;
            _task = Task.Factory.StartNew(RenderingLoop, _cancelationToken, _cancelationToken);
        }

        private void StopTask()
        {
            try
            {
                Log.Debug("SurfaceViewRenderAdapter", $"StopTask _cancelationTokenSource != null {_cancelationTokenSource != null}");
                if (_cancelationTokenSource != null)
                {
                    _cancelationTokenSource.Cancel();
                    Task.WaitAll(_task);
                    _cancelationTokenSource = null;
                    _task = null;
                }
            }
            catch (Exception e)
            {
                Log.Debug("SurfaceViewRenderAdapter", $"Exception in StopTask DrawingTask.Cancel {e}");
            }
        }
    }
}