using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Gtk3
{
    abstract class WindowBaseImpl : IWindowBaseImpl, IPlatformHandle
    {
        public readonly GtkWindow GtkWidget;
        private IInputRoot _inputRoot;
        private readonly GtkImContext _imContext;
        private readonly FramebufferManager _framebuffer;
        protected readonly List<IDisposable> Disposables = new List<IDisposable>();
        private Size _lastSize;
        private Point _lastPosition;
        private double _lastScaling;
        private uint _lastKbdEvent;
        private uint _lastSmoothScrollEvent;
        private object _lock = new object();
        private IDeferredRenderOperation _nextRenderOperation;
        private readonly AutoResetEvent _canSetNextOperation = new AutoResetEvent(true);
        internal IntPtr GdkWindowHandle;
        private bool _overrideRedirect;

        public WindowBaseImpl(GtkWindow gtkWidget)
        {
            GtkWidget = gtkWidget;
            _framebuffer = new FramebufferManager(this);
            _imContext = Native.GtkImMulticontextNew();
            Disposables.Add(_imContext);
            Native.GtkWidgetSetEvents(gtkWidget, 0xFFFFFE);
            Disposables.Add(Signal.Connect<Native.D.signal_commit>(_imContext, "commit", OnCommit));

            Disposables.Add(EventManager.ConnectEvents(this));

            Native.GtkWidgetRealize(gtkWidget);
            GdkWindowHandle = this.Handle.Handle;
            _lastSize = ClientSize;
        }

        public void OnConfigured()
        {
            int w, h;
            if (!OverrideRedirect)
            {
                Native.GtkWindowGetSize(GtkWidget, out w, out h);
                var size = ClientSize = new Size(w, h);
                if (_lastSize != size)
                {
                    Resized?.Invoke(size);
                    _lastSize = size;
                }
            }
            var pos = Position;
            if (_lastPosition != pos)
            {
                PositionChanged?.Invoke(pos);
                _lastPosition = pos;
            }
            var scaling = Scaling;
            if (_lastScaling != scaling)
            {
                ScalingChanged?.Invoke(scaling);
                _lastScaling = scaling;
            }
        }

        internal unsafe bool FilterKeypress(GdkEventKey* evnt)
        {
            _lastKbdEvent = evnt->time;
            if (Native.GtkImContextFilterKeypress(_imContext, new IntPtr(evnt)))
                return true;

            return false;
        }

        internal bool FilterScroll(uint time, GdkScrollDirection direction)
        {
            if (time - _lastSmoothScrollEvent < 10 && direction != GdkScrollDirection.Smooth)
                return true;

            if (direction == GdkScrollDirection.Smooth)
                _lastSmoothScrollEvent = time;

            return false;
        }

        private unsafe bool OnCommit(IntPtr gtkwidget, IntPtr utf8string, IntPtr userdata)
        {
            OnInput(new RawTextInputEventArgs(Gtk3Platform.Keyboard, _lastKbdEvent, Utf8Buffer.StringFromPtr(utf8string)));
            return true;
        }

        public void OnRealized()
        {
            Native.GtkImContextSetClientWindow(_imContext, Native.GtkWidgetGetWindow(GtkWidget));
        }

        internal IntPtr CurrentCairoContext { get; private set; }

        public void OnDraw(IntPtr cairocontext)
        {
            if (!Gtk3Platform.UseDeferredRendering)
            {
                CurrentCairoContext = cairocontext;
                Paint?.Invoke(new Rect(ClientSize));
                CurrentCairoContext = IntPtr.Zero;
            }
        }


        public void SetNextRenderOperation(IDeferredRenderOperation op)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_nextRenderOperation == null)
                    {
                        _nextRenderOperation = op;
                        return;
                    }
                }
                _canSetNextOperation.WaitOne();
            }
            
        }

        public void OnRenderTick()
        {
            IDeferredRenderOperation op = null;
            lock (_lock)
            {
                if (_nextRenderOperation != null)
                {
                    op = _nextRenderOperation;
                    _nextRenderOperation = null;
                }
                _canSetNextOperation.Set();
            }
            if (op != null)
            {
                op?.RenderNow(null);
                op?.Dispose();
            }
        }


        public void Dispose() => DoDispose(false);
        
        public void DoDispose(bool fromDestroy)
        {
            foreach (var d in Disposables.AsEnumerable().Reverse())
                d.Dispose();

            if (!GtkWidget.IsClosed)
                Closed?.Invoke();
            
            Disposables.Clear();

            if (!fromDestroy && !GtkWidget.IsClosed)
                Native.GtkWindowClose(GtkWidget);

            GtkWidget.Dispose();
        }

        public Size MaxClientSize
        {
            get
            {
                var s = Native.GtkWidgetGetScreen(GtkWidget);
                return new Size(Native.GdkScreenGetWidth(s), Native.GdkScreenGetHeight(s));
            }
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            if (GtkWidget.IsClosed)
                return;

            GdkGeometry geometry = new GdkGeometry();
            geometry.min_width = minSize.Width > 0 ? (int)minSize.Width : -1;
            geometry.min_height = minSize.Height > 0 ? (int)minSize.Height : -1;
            geometry.max_width = !Double.IsInfinity(maxSize.Width) && maxSize.Width > 0 ? (int)maxSize.Width : 999999;
            geometry.max_height = !Double.IsInfinity(maxSize.Height) && maxSize.Height > 0 ? (int)maxSize.Height : 999999;

            Native.GtkWindowSetGeometryHints(GtkWidget, IntPtr.Zero, ref geometry, GdkWindowHints.GDK_HINT_MIN_SIZE | GdkWindowHints.GDK_HINT_MAX_SIZE);
        } 

        public IMouseDevice MouseDevice => Gtk3Platform.Mouse;

        public double Scaling => LastKnownScaleFactor = (int) (Native.GtkWidgetGetScaleFactor?.Invoke(GtkWidget) ?? 1);

        public IPlatformHandle Handle => this;

        string IPlatformHandle.HandleDescriptor => "HWND";

        public Action Activated { get; set; }
        public Func<bool> Closing { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }

        internal virtual void OnStateChanged(GdkWindowState changed_mask, GdkWindowState new_window_state)
        {
            if (changed_mask.HasFlag(GdkWindowState.Focused))
            {
                if (new_window_state.HasFlag(GdkWindowState.Focused))
                    Activated?.Invoke();
                else
                    Deactivated?.Invoke();
            }
        }

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; } //TODO
        public Action<Point> PositionChanged { get; set; }

        public void Activate() => Native.GtkWidgetActivate(GtkWidget);

        public void Invalidate(Rect rect)
        {
            if(GtkWidget.IsClosed)
                return;
            var s = ClientSize;
            Native.GtkWidgetQueueDrawArea(GtkWidget, 0, 0, (int) s.Width, (int) s.Height);
        }

        public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;
        public IInputRoot GetInputRoot() => _inputRoot;

        public void OnInput(RawInputEventArgs args)
        {
            Dispatcher.UIThread.Post(() => Input?.Invoke(args), DispatcherPriority.Input);
        }

        public Point PointToClient(Point point)
        {
            int x, y;
            Native.GdkWindowGetOrigin(Native.GtkWidgetGetWindow(GtkWidget), out x, out y);

            return new Point(point.X - x, point.Y - y);
        }

        public Point PointToScreen(Point point)
        {
            int x, y;
            Native.GdkWindowGetOrigin(Native.GtkWidgetGetWindow(GtkWidget), out x, out y);
            return new Point(point.X + x, point.Y + y);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            if (GtkWidget.IsClosed)
                return;
            Native.GdkWindowSetCursor(Native.GtkWidgetGetWindow(GtkWidget), cursor?.Handle ??  IntPtr.Zero);
        }

        public void Show() => Native.GtkWindowPresent(GtkWidget);

        public void Hide() => Native.GtkWidgetHide(GtkWidget);

        public void SetTopmost(bool value) => Native.GtkWindowSetKeepAbove(GtkWidget, value);

        void GetGlobalPointer(out int x, out int y)
        {
            int mask;
            Native.GdkWindowGetPointer(Native.GdkScreenGetRootWindow(Native.GtkWidgetGetScreen(GtkWidget)),
                out x, out y, out mask);
        }

        public void BeginMoveDrag()
        {
            int x, y;
            GetGlobalPointer(out x, out y);
            Native.GdkWindowBeginMoveDrag(Native.GtkWidgetGetWindow(GtkWidget), 1, x, y, 0);
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            int x, y;
            GetGlobalPointer(out x, out y);
            Native.GdkWindowBeginResizeDrag(Native.GtkWidgetGetWindow(GtkWidget), edge, 1, x, y, 0);
        }


        public Size ClientSize { get; private set; }
        public int LastKnownScaleFactor { get; private set; }

        public void Resize(Size value)
        {
            if (GtkWidget.IsClosed)
                return;
         
            Native.GtkWindowResize(GtkWidget, (int)value.Width, (int)value.Height);
            if (OverrideRedirect)
            {
                var size = ClientSize = value;
                if (_lastSize != size)
                {
                    Resized?.Invoke(size);
                    _lastSize = size;
                }
            }
        }

        public bool OverrideRedirect
        {
            get => _overrideRedirect;
            set
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Native.GdkWindowSetOverrideRedirect(Native.GtkWidgetGetWindow(GtkWidget), value);
                    _overrideRedirect = value;
                }
            }
        }
        
        public IScreenImpl Screen
        {
            get;
        } = new ScreenImpl();

        public Point Position
        {
            get
            {
                int x, y;
                Native.GtkWindowGetPosition(GtkWidget, out x, out y);
                return new Point(x, y);
            }
            set { Native.GtkWindowMove(GtkWidget, (int)value.X, (int)value.Y); }
        }

        IntPtr IPlatformHandle.Handle => Native.GetNativeGdkWindowHandle(Native.GtkWidgetGetWindow(GtkWidget));
        public IEnumerable<object> Surfaces => new object[] {Handle, _framebuffer};

        public IRenderer CreateRenderer(IRenderRoot root)
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            return Gtk3Platform.UseDeferredRendering
                ? (IRenderer) new DeferredRenderer(root, loop)
                : new ImmediateRenderer(root);
        }
    }
}
