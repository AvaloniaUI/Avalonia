using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;

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

        public WindowBaseImpl(GtkWindow gtkWidget)
        {
            
            GtkWidget = gtkWidget;
            Disposables.Add(gtkWidget);
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
            Connect<Native.D.signal_generic>("destroy", OnDestroy);
            Native.GtkWidgetRealize(gtkWidget);
            _lastSize = ClientSize;
        }

        private bool OnConfigured(IntPtr gtkwidget, IntPtr ev, IntPtr userdata)
        {
            var size = ClientSize;
            if (_lastSize != size)
            {
                Resized?.Invoke(size);
                _lastSize = size;
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
            Dispose();
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
            Input?.Invoke(e);
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
            Input(e);
            
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
            Input(e);
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
            Input(e);
            return true;
        }

        private unsafe bool OnLeaveNotifyEvent(IntPtr w, IntPtr pev, IntPtr userData)
        {
            var evnt = (GdkEventCrossing*) pev;
            var position = new Point(evnt->x, evnt->y);
            Input(new RawMouseEventArgs(Gtk3Platform.Mouse,
                evnt->time,
                _inputRoot,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt->state)));
            return true;
        }

        private unsafe bool OnCommit(IntPtr gtkwidget, IntPtr utf8string, IntPtr userdata)
        {
            Input(new RawTextInputEventArgs(Gtk3Platform.Keyboard, _lastKbdEvent, Utf8Buffer.StringFromPtr(utf8string)));
            return true;
        }

        void ConnectEvent(string name, Native.D.signal_onevent handler) 
            => Disposables.Add(Signal.Connect<Native.D.signal_onevent>(GtkWidget, name, handler));
        void Connect<T>(string name, T handler) => Disposables.Add(Signal.Connect(GtkWidget, name, handler));

        internal IntPtr CurrentCairoContext { get; private set; }

        private bool OnDraw(IntPtr gtkwidget, IntPtr cairocontext, IntPtr userdata)
        {
            CurrentCairoContext = cairocontext;
            Paint?.Invoke(new Rect(ClientSize));
            CurrentCairoContext = IntPtr.Zero;
            return true;
        }

        public void Dispose()
        {
            //We are calling it here, since signal handler will be detached
            if (!GtkWidget.IsClosed)
                Closed?.Invoke();
            foreach(var d in Disposables.AsEnumerable().Reverse())
                d.Dispose();
            Disposables.Clear();
        }

        public Size MaxClientSize
        {
            get
            {
                var s = Native.GtkWidgetGetScreen(GtkWidget);
                return new Size(Native.GdkScreenGetWidth(s), Native.GdkScreenGetHeight(s));
            }
        }

        public IMouseDevice MouseDevice => Gtk3Platform.Mouse;

        public double Scaling => (double) 1 / (Native.GtkWidgetGetScaleFactor?.Invoke(GtkWidget) ?? 1);

        public IPlatformHandle Handle => this;

        string IPlatformHandle.HandleDescriptor => "HWND";

        public Action Activated { get; set; }
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
            Native.GtkWidgetQueueDrawArea(GtkWidget, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }

        public void SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

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


        public Size ClientSize
        {
            get
            {
                if (GtkWidget.IsClosed)
                    return new Size();
                int w, h;
                Native.GtkWindowGetSize(GtkWidget, out w, out h);
                return new Size(w, h);
            }
        }

        public void Resize(Size value)
        {
            if (GtkWidget.IsClosed)
                return;
            Native.GtkWindowResize(GtkWidget, (int)value.Width, (int)value.Height);
        }

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
            return new ImmediateRenderer(root);
        }
    }
}
