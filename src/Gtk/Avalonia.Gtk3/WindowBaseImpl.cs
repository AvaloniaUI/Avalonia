using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        private GCHandle _gcHandle;
        private object _lock = new object();
        private IDeferredRenderOperation _nextRenderOperation;
        private readonly AutoResetEvent _canSetNextOperation = new AutoResetEvent(true);
        internal IntPtr? GdkWindowHandle;
        private bool _overrideRedirect;
        private uint? _tickCallback;
        public WindowBaseImpl(GtkWindow gtkWidget)
        {
            
            GtkWidget = gtkWidget;
            _framebuffer = new FramebufferManager(this);
            _imContext = Native.GtkImMulticontextNew();
            Disposables.Add(_imContext);
            Native.GtkWidgetSetEvents(gtkWidget, 0xFFFFFE);
            Disposables.Add(Signal.Connect<Native.D.signal_commit>(_imContext, "commit", OnCommit));
            Connect<Native.D.signal_widget_draw>("draw", OnDraw);
            Connect<Native.D.signal_generic>("realize", OnRealized);
            ConnectEvent("configure-event", OnConfigured);
            ConnectEvent("button-press-event", OnButton);
            ConnectEvent("button-release-event", OnButton);
            ConnectEvent("motion-notify-event", OnMotion);
            ConnectEvent("scroll-event", OnScroll);
            ConnectEvent("window-state-event", OnStateChanged);
            ConnectEvent("key-press-event", OnKeyEvent);
            ConnectEvent("key-release-event", OnKeyEvent);
            ConnectEvent("leave-notify-event", OnLeaveNotifyEvent);
            ConnectEvent("delete-event", OnClosingEvent);
            Connect<Native.D.signal_generic>("destroy", OnDestroy);
            Native.GtkWidgetRealize(gtkWidget);
            GdkWindowHandle = this.Handle.Handle;
            _lastSize = ClientSize;
            if (Gtk3Platform.UseDeferredRendering)
            {
                Native.GtkWidgetSetDoubleBuffered(gtkWidget, false);
                _gcHandle = GCHandle.Alloc(this);
                _tickCallback = Native.GtkWidgetAddTickCallback(GtkWidget, PinnedStaticCallback, GCHandle.ToIntPtr(_gcHandle), IntPtr.Zero);
                
            }
        }

        private bool OnConfigured(IntPtr gtkwidget, IntPtr ev, IntPtr userdata)
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
            return false;
        }

        private bool OnRealized(IntPtr gtkwidget, IntPtr userdata)
        {
            Native.GtkImContextSetClientWindow(_imContext, Native.GtkWidgetGetWindow(GtkWidget));
            return false;
        }

        private bool OnDestroy(IntPtr gtkwidget, IntPtr userdata)
        {
            DoDispose(true);
            return false;
        }

        private static InputModifiers GetModifierKeys(GdkModifierType state)
        {
            var rv = InputModifiers.None;
            if (state.HasFlag(GdkModifierType.ControlMask))
                rv |= InputModifiers.Control;
            if (state.HasFlag(GdkModifierType.ShiftMask))
                rv |= InputModifiers.Shift;
            if (state.HasFlag(GdkModifierType.Mod1Mask))
                rv |= InputModifiers.Control;
            if (state.HasFlag(GdkModifierType.Button1Mask))
                rv |= InputModifiers.LeftMouseButton;
            if (state.HasFlag(GdkModifierType.Button2Mask))
                rv |= InputModifiers.RightMouseButton;
            if (state.HasFlag(GdkModifierType.Button3Mask))
                rv |= InputModifiers.MiddleMouseButton;
            return rv;
        }

        private unsafe bool OnClosingEvent(IntPtr w, IntPtr ev, IntPtr userdata)
        {
            bool? preventClosing = Closing?.Invoke();
            return preventClosing ?? false;
        }

        private unsafe bool OnButton(IntPtr w, IntPtr ev, IntPtr userdata)
        {
            var evnt = (GdkEventButton*)ev;
            var e = new RawMouseEventArgs(
                Gtk3Platform.Mouse,
                evnt->time,
                _inputRoot,
                evnt->type == GdkEventType.ButtonRelease
                    ? evnt->button == 1
                        ? RawMouseEventType.LeftButtonUp
                        : evnt->button == 3 ? RawMouseEventType.RightButtonUp : RawMouseEventType.MiddleButtonUp
                    : evnt->button == 1
                        ? RawMouseEventType.LeftButtonDown
                        : evnt->button == 3 ? RawMouseEventType.RightButtonDown : RawMouseEventType.MiddleButtonDown,
                new Point(evnt->x, evnt->y), GetModifierKeys(evnt->state));
            OnInput(e);
            return true;
        }

        protected virtual unsafe bool OnStateChanged(IntPtr w, IntPtr pev, IntPtr userData)
        {
            var ev = (GdkEventWindowState*) pev;
            if (ev->changed_mask.HasFlag(GdkWindowState.Focused))
            {
                if(ev->new_window_state.HasFlag(GdkWindowState.Focused))
                    Activated?.Invoke();
                else
                    Deactivated?.Invoke();
            }
            return true;
        }

        private unsafe bool OnMotion(IntPtr w, IntPtr ev, IntPtr userdata)
        {
            var evnt = (GdkEventMotion*)ev;
            var position = new Point(evnt->x, evnt->y);
            Native.GdkEventRequestMotions(ev);
            var e = new RawMouseEventArgs(
                Gtk3Platform.Mouse,
                evnt->time,
                _inputRoot,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt->state));
            OnInput(e);
            
            return true;
        }
        private unsafe bool OnScroll(IntPtr w, IntPtr ev, IntPtr userdata)
        {
            var evnt = (GdkEventScroll*)ev;

            //Ignore duplicates
            if (evnt->time - _lastSmoothScrollEvent < 10 && evnt->direction != GdkScrollDirection.Smooth)
                return true;

            var delta = new Vector();
            const double step = (double) 1;
            if (evnt->direction == GdkScrollDirection.Down)
                delta = new Vector(0, -step);
            else if (evnt->direction == GdkScrollDirection.Up)
                delta = new Vector(0, step);
            else if (evnt->direction == GdkScrollDirection.Right)
                delta = new Vector(-step, 0);
            else if (evnt->direction == GdkScrollDirection.Left)
                delta = new Vector(step, 0);
            else if (evnt->direction == GdkScrollDirection.Smooth)
            {
                delta = new Vector(-evnt->delta_x, -evnt->delta_y);
                _lastSmoothScrollEvent = evnt->time;
            }
            var e = new RawMouseWheelEventArgs(Gtk3Platform.Mouse, evnt->time, _inputRoot,
                new Point(evnt->x, evnt->y), delta, GetModifierKeys(evnt->state));
            OnInput(e);
            return true;
        }

        private unsafe bool OnKeyEvent(IntPtr w, IntPtr pev, IntPtr userData)
        {
            var evnt = (GdkEventKey*) pev;
            _lastKbdEvent = evnt->time;
            if (Native.GtkImContextFilterKeypress(_imContext, pev))
                return true;
            var e = new RawKeyEventArgs(
                Gtk3Platform.Keyboard,
                evnt->time,
                evnt->type == GdkEventType.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                Avalonia.Gtk.Common.KeyTransform.ConvertKey((GdkKey)evnt->keyval), GetModifierKeys((GdkModifierType)evnt->state));
            OnInput(e);
            return true;
        }

        private unsafe bool OnLeaveNotifyEvent(IntPtr w, IntPtr pev, IntPtr userData)
        {
            var evnt = (GdkEventCrossing*) pev;
            var position = new Point(evnt->x, evnt->y);
            OnInput(new RawMouseEventArgs(Gtk3Platform.Mouse,
                evnt->time,
                _inputRoot,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt->state)));
            return true;
        }

        private unsafe bool OnCommit(IntPtr gtkwidget, IntPtr utf8string, IntPtr userdata)
        {
            OnInput(new RawTextInputEventArgs(Gtk3Platform.Keyboard, _lastKbdEvent, Utf8Buffer.StringFromPtr(utf8string)));
            return true;
        }

        protected void ConnectEvent(string name, Native.D.signal_onevent handler) 
            => Disposables.Add(Signal.Connect<Native.D.signal_onevent>(GtkWidget, name, handler));
        void Connect<T>(string name, T handler) => Disposables.Add(Signal.Connect(GtkWidget, name, handler));

        internal IntPtr CurrentCairoContext { get; private set; }

        private bool OnDraw(IntPtr gtkwidget, IntPtr cairocontext, IntPtr userdata)
        {
            if (!Gtk3Platform.UseDeferredRendering)
            {
                CurrentCairoContext = cairocontext;
                Paint?.Invoke(new Rect(ClientSize));
                CurrentCairoContext = IntPtr.Zero;
            }
            return true;
        }

        private static Native.D.TickCallback PinnedStaticCallback = StaticTickCallback;

        static bool StaticTickCallback(IntPtr widget, IntPtr clock, IntPtr userData)
        {
            var impl = (WindowBaseImpl) GCHandle.FromIntPtr(userData).Target;
            impl.OnRenderTick();
            return true;
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

        private void OnRenderTick()
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
        
        void DoDispose(bool fromDestroy)
        {
            if (_tickCallback.HasValue)
            {
                if (!GtkWidget.IsClosed)
                    Native.GtkWidgetRemoveTickCallback(GtkWidget, _tickCallback.Value);
                _tickCallback = null;
            }
            
            //We are calling it here, since signal handler will be detached
            if (!GtkWidget.IsClosed)
                Closed?.Invoke();
            foreach(var d in Disposables.AsEnumerable().Reverse())
                d.Dispose();
            Disposables.Clear();
            
            if (!fromDestroy && !GtkWidget.IsClosed)
                Native.GtkWindowClose(GtkWidget);
            GtkWidget.Dispose();
            
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
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

        void OnInput(RawInputEventArgs args)
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
