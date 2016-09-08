using System;
using System.Reactive.Disposables;
using Avalonia.Platform;
using Gdk;

namespace Avalonia.Gtk
{
    using Gtk = global::Gtk;
    public class WindowImpl : WindowImplBase
    {
        private Gtk.Window _window;
        private Gtk.Window Window => _window ?? (_window = (Gtk.Window) Widget);


<<<<<<< HEAD
        private Point _lastPosition;

        private Gtk.IMContext _imContext;
=======
>>>>>>> dfd4bbf34a7632465a84126b22495072945fa7d5

        public WindowImpl(Gtk.WindowType type) : base(new PlatformHandleAwareWindow(type))
        {
            Init();
        }

        public WindowImpl()
            : base(new PlatformHandleAwareWindow(Gtk.WindowType.Toplevel) {DefaultSize = new Gdk.Size(900, 480)})
        {
            Init();
        }

        void Init()
        {
            Window.FocusActivated += OnFocusActivated;
            Window.ConfigureEvent += OnConfigureEvent;
            _lastClientSize = ClientSize;
            _lastPosition = Position;
        }
        private Size _lastClientSize;
        void OnConfigureEvent(object o, Gtk.ConfigureEventArgs args)
        {
            var evnt = args.Event;
            args.RetVal = true;
            var newSize = new Size(evnt.Width, evnt.Height);

            if (newSize != _lastClientSize)
            {
                Resized(newSize);
                _lastClientSize = newSize;
            }
        }

        public override Size ClientSize
        {
            get
            {
                int width;
                int height;
                Window.GetSize(out width, out height);
                return new Size(width, height);
            }

            set
            {
                Window.Resize((int)value.Width, (int)value.Height);
            }
        }

        public override void SetTitle(string title)
        {
            Window.Title = title;
        }

<<<<<<< HEAD

        IntPtr IPlatformHandle.Handle => GetNativeWindow();
        public string HandleDescriptor => "HWND";

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public Action<Point> PositionChanged { get; set; }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public void Invalidate(Rect rect)
        {
            if (base.GdkWindow != null)
                base.GdkWindow.InvalidateRect(
                    new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height), true);
        }

        public Point PointToClient(Point point)
=======
        void OnFocusActivated(object sender, EventArgs eventArgs)
>>>>>>> dfd4bbf34a7632465a84126b22495072945fa7d5
        {
            Activated();
        }

        public override void BeginMoveDrag()
        {
            int x, y;
            ModifierType mod;
            Window.Screen.RootWindow.GetPointer(out x, out y, out mod);
            Window.BeginMoveDrag(1, x, y, 0);
        }

        public override void BeginResizeDrag(Controls.WindowEdge edge)
        {
            int x, y;
            ModifierType mod;
            Window.Screen.RootWindow.GetPointer(out x, out y, out mod);
            Window.BeginResizeDrag((Gdk.WindowEdge)(int)edge, 1, x, y, 0);
        }

        public override Point Position
        {
            get
            {
                int x, y;
                Window.GetPosition(out x, out y);
                return new Point(x, y);
            }
            set
            {
                Window.Move((int)value.X, (int)value.Y);
            }
        }

        public override IDisposable ShowDialog()
        {
            Window.Modal = true;
            Window.Show();

            return Disposable.Empty;
        }

<<<<<<< HEAD
        public void SetSystemDecorations(bool enabled) => Decorated = enabled;

        void ITopLevelImpl.Activate()
        {
            Activate();
        }

        private static InputModifiers GetModifierKeys(ModifierType state)
        {
            var rv = InputModifiers.None;
            if (state.HasFlag(ModifierType.ControlMask))
                rv |= InputModifiers.Control;
            if (state.HasFlag(ModifierType.ShiftMask))
                rv |= InputModifiers.Shift;
            if (state.HasFlag(ModifierType.Mod1Mask))
                rv |= InputModifiers.Control;
            if(state.HasFlag(ModifierType.Button1Mask))
                rv |= InputModifiers.LeftMouseButton;
            if (state.HasFlag(ModifierType.Button2Mask))
                rv |= InputModifiers.RightMouseButton;
            if (state.HasFlag(ModifierType.Button3Mask))
                rv |= InputModifiers.MiddleMouseButton;
            return rv;
        }

        protected override bool OnButtonPressEvent(EventButton evnt)
        {

            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                _inputRoot,
                evnt.Button == 1
                    ? RawMouseEventType.LeftButtonDown
                    : evnt.Button == 3 ? RawMouseEventType.RightButtonDown : RawMouseEventType.MiddleButtonDown,
                new Point(evnt.X, evnt.Y), GetModifierKeys(evnt.State));
            Input(e);
            return true;
        }

        protected override bool OnScrollEvent(EventScroll evnt)
        {
            double step = 1;
            var delta = new Vector();
            if (evnt.Direction == ScrollDirection.Down)
                delta = new Vector(0, -step);
            else if (evnt.Direction == ScrollDirection.Up)
                delta = new Vector(0, step);
            else if (evnt.Direction == ScrollDirection.Right)
                delta = new Vector(-step, 0);
            if (evnt.Direction == ScrollDirection.Left)
                delta = new Vector(step, 0);
            var e = new RawMouseWheelEventArgs(GtkMouseDevice.Instance, evnt.Time, _inputRoot, new Point(evnt.X, evnt.Y), delta, GetModifierKeys(evnt.State));
            Input(e);
            return base.OnScrollEvent(evnt);
        }

        protected override bool OnButtonReleaseEvent(EventButton evnt)
        {
            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                _inputRoot,
                evnt.Button == 1
                    ? RawMouseEventType.LeftButtonUp
                    : evnt.Button == 3 ? RawMouseEventType.RightButtonUp : RawMouseEventType.MiddleButtonUp,
                new Point(evnt.X, evnt.Y), GetModifierKeys(evnt.State));
            Input(e);
            return true;
        }

        protected override bool OnConfigureEvent(EventConfigure evnt)
        {
            var newSize = new Size(evnt.Width, evnt.Height);

            if (newSize != _lastClientSize)
            {
                Resized(newSize);
                _lastClientSize = newSize;
            }

            var newPosition = new Point(evnt.X, evnt.Y);

            if (newPosition != _lastPosition)
            {
                PositionChanged(newPosition);
                _lastPosition = newPosition;
            }

            return true;
        }

        protected override void OnDestroyed()
        {
            Closed();
        }

        private bool ProcessKeyEvent(EventKey evnt)
        {
            _lastKeyEventTimestamp = evnt.Time;
            if (_imContext.FilterKeypress(evnt))
                return true;
            var e = new RawKeyEventArgs(
                GtkKeyboardDevice.Instance,
                evnt.Time,
                evnt.Type == EventType.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                GtkKeyboardDevice.ConvertKey(evnt.Key), GetModifierKeys(evnt.State));
            Input(e);
            return true;
        }

        protected override bool OnKeyPressEvent(EventKey evnt) => ProcessKeyEvent(evnt);

        protected override bool OnKeyReleaseEvent(EventKey evnt) => ProcessKeyEvent(evnt);

        private void ImContext_Commit(object o, Gtk.CommitArgs args)
        {
            Input(new RawTextInputEventArgs(GtkKeyboardDevice.Instance, _lastKeyEventTimestamp, args.Str));
        }

        protected override bool OnExposeEvent(EventExpose evnt)
        {
            Paint(evnt.Area.ToAvalonia());
            return true;
        }

        protected override void OnFocusActivated()
        {
            Activated();
        }

        protected override bool OnMotionNotifyEvent(EventMotion evnt)
        {
            var position = new Point(evnt.X, evnt.Y);

            GtkMouseDevice.Instance.SetClientPosition(position);

            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                _inputRoot,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt.State));
            Input(e);
            return true;
        }
=======
        public override void SetSystemDecorations(bool enabled) => Window.Decorated = enabled;
>>>>>>> dfd4bbf34a7632465a84126b22495072945fa7d5

        public override void SetIcon(IWindowIconImpl icon)
        {
            Window.Icon = ((IconImpl)icon).Pixbuf;
        }
    }
}