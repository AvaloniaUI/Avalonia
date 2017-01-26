using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    abstract class TopLevelImpl : ITopLevelImpl, IPlatformHandle
    {
        protected readonly IntPtr GtkWidget;
        private IInputRoot _inputRoot;
        protected readonly List<IDisposable> _disposables = new List<IDisposable>();

        public TopLevelImpl(IntPtr gtkWidget)
        {
            GtkWidget = gtkWidget;
            Native.GtkWidgetSetEvents(gtkWidget, uint.MaxValue);
            Native.GtkWidgetRealize(gtkWidget);
            Connect<Native.D.signal_widget_draw>("draw", OnDraw);
            Connect<Native.D.signal_onevent>("configure-event", OnConfigured);
            Connect<Native.D.signal_onevent>("button-press-event", OnButton);
            Connect<Native.D.signal_onevent>("button-release-event", OnButton);
            Connect<Native.D.signal_onevent>("motion-notify-event", OnMotion);
            Connect<Native.D.signal_onevent>("scroll-event", OnScroll);
        }

        private Size _lastSize;
        private Point _lastPosition;

        private bool OnConfigured(IntPtr gtkwidget, IntPtr ev, IntPtr userdata)
        {
            var size = ClientSize;
            if (_lastSize != size)
            {
                _lastSize = size;
                Resized?.Invoke(size);
            }
            var pos = Position;
            if (_lastPosition != pos)
            {
                _lastPosition = pos;
                PositionChanged?.Invoke(pos);
            }

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
            return false;
        }

        private unsafe bool OnMotion(IntPtr w, IntPtr ev, IntPtr userdata)
        {
            var evnt = (GdkEventMotion*)ev;
            var position = new Point(evnt->x, evnt->y);
            

            var e = new RawMouseEventArgs(
                Gtk3Platform.Mouse,
                evnt->time,
                _inputRoot,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt->state));
            Input(e);
            return false;
        }
        private unsafe bool OnScroll(IntPtr w, IntPtr ev, IntPtr userdata)
        {
            var evnt = (GdkEventScroll*)ev;
            var delta = new Vector();
            var step = (double) 1;
            if (evnt->direction == GdkScrollDirection.Down)
                delta = new Vector(0, -step);
            else if (evnt->direction == GdkScrollDirection.Up)
                delta = new Vector(0, step);
            else if (evnt->direction == GdkScrollDirection.Right)
                delta = new Vector(-step, 0);
            else if (evnt->direction == GdkScrollDirection.Left)
                delta = new Vector(step, 0);
            else if (evnt->direction == GdkScrollDirection.Smooth)
                delta = new Vector(evnt->delta_x, evnt->delta_y);

            var e = new RawMouseWheelEventArgs(Gtk3Platform.Mouse, evnt->time, _inputRoot,
                new Point(evnt->x, evnt->y), delta, GetModifierKeys(evnt->state));
            Input(e);
            return false;
        }

        void Connect<T>(string name, T handler) => _disposables.Add(Signal.Connect<T>(GtkWidget, name, handler));

        private bool OnDraw(IntPtr gtkwidget, IntPtr cairocontext, IntPtr userdata)
        {
            Paint?.Invoke(new Rect(ClientSize));
            return true;
        }

        public void Dispose()
        {
            foreach(var d in _disposables)
                d.Dispose();
            _disposables.Clear();
            //TODO
        }

        public Size MaxClientSize
        {
            get
            {
                var s = Native.GtkWidgetGetScreen(GtkWidget);
                return new Size(Native.GdkScreenGetWidth(s), Native.GdkScreenGetHeight(s));
            }
        }


        public double Scaling => 1; //TODO: Implement scaling
        public IPlatformHandle Handle => this;

        string IPlatformHandle.HandleDescriptor => "HWND";

        public Action Activated { get; set; } //TODO
        public Action Closed { get; set; } //TODO
        public Action Deactivated { get; set; } //TODO
        public Action<RawInputEventArgs> Input { get; set; } //TODO
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public Action<double> ScalingChanged { get; set; } //TODO
        public Action<Point> PositionChanged { get; set; }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
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
            //STUB
        }

        public void Show() => Native.GtkWindowPresent(GtkWidget);

        public void Hide() => Native.GtkWidgetHide(GtkWidget);

        public void BeginMoveDrag()
        {
            //STUB
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //STUB
        }


        public Size ClientSize
        {
            get
            {
                int w, h;
                Native.GtkWindowGetSize(GtkWidget, out w, out h);
                return new Size(w, h);
            }
            set { Native.GtkWindowResize(GtkWidget, (int)value.Width, (int)value.Height); }
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
        public IEnumerable<object> Surfaces => new object[] {Handle};
    }
}
