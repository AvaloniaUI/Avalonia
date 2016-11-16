// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Gdk;
using Avalonia.Controls;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Input;
using Avalonia.Threading;
using Action = System.Action;
using WindowEdge = Avalonia.Controls.WindowEdge;

namespace Avalonia.Gtk
{
    using Gtk = global::Gtk;

    public abstract class WindowImplBase : IWindowImpl
    {
        private IInputRoot _inputRoot;
        protected Gtk.Widget _window;
        public Gtk.Widget Widget => _window;


        private Gtk.IMContext _imContext;

        private uint _lastKeyEventTimestamp;

        private static readonly Gdk.Cursor DefaultCursor = new Gdk.Cursor(CursorType.LeftPtr);

        protected WindowImplBase(Gtk.Widget window)
        {
            _window = window;
            Init();
        }

        void Init()
        {
            Handle = _window as IPlatformHandle;
            _window.Events = EventMask.AllEventsMask;
            _imContext = new Gtk.IMMulticontext();
            _imContext.Commit += ImContext_Commit;
            _window.Realized += OnRealized;
            _window.DoubleBuffered = false;
            _window.Realize();
            _window.ButtonPressEvent += OnButtonPressEvent;
            _window.ButtonReleaseEvent += OnButtonReleaseEvent;
            _window.ScrollEvent += OnScrollEvent;
            _window.Destroyed += OnDestroyed;
            _window.KeyPressEvent += OnKeyPressEvent;
            _window.KeyReleaseEvent += OnKeyReleaseEvent;
            _window.ExposeEvent += OnExposeEvent;
            _window.MotionNotifyEvent += OnMotionNotifyEvent;
            
        }

        public IPlatformHandle Handle { get; private set; }

        void OnRealized (object sender, EventArgs eventArgs)
        {
            _imContext.ClientWindow = _window.GdkWindow;
        }

        public abstract Size ClientSize { get; set; }


        public Size MaxClientSize
        {
            get
            {
                // TODO: This should take into account things such as taskbar and window border
                // thickness etc.
                return new Size(_window.Screen.Width, _window.Screen.Height);
            }
        }

        public Avalonia.Controls.WindowState WindowState
        {
            get
            {
                switch (_window.GdkWindow.State)
                {
                    case Gdk.WindowState.Iconified:
                        return Controls.WindowState.Minimized;
                    case Gdk.WindowState.Maximized:
                        return Controls.WindowState.Maximized;
                    default:
                        return Controls.WindowState.Normal;
                }
            }

            set
            {
                switch (value)
                {
                    case Controls.WindowState.Minimized:
                        _window.GdkWindow.Iconify();
                        break;
                    case Controls.WindowState.Maximized:
                        _window.GdkWindow.Maximize();
                        break;
                    case Controls.WindowState.Normal:
                        _window.GdkWindow.Deiconify();
                        _window.GdkWindow.Unmaximize();
                        break;
                }
            }
        }

        public double Scaling => 1;

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }
		
        public Action<Point> PositionChanged { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public void Invalidate(Rect rect)
        {
            if (_window?.GdkWindow != null)
                _window.GdkWindow.InvalidateRect(
                    new Rectangle((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height), true);
        }

        public Point PointToClient(Point point)
        {
            int x, y;
            _window.GdkWindow.GetDeskrelativeOrigin(out x, out y);

            return new Point(point.X - x, point.Y - y);
        }

        public Point PointToScreen(Point point)
        {
            int x, y;
            _window.GdkWindow.GetDeskrelativeOrigin(out x, out y);
            return new Point(point.X + x, point.Y + y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public abstract void SetTitle(string title);
        public abstract IDisposable ShowDialog();
        public abstract void SetSystemDecorations(bool enabled);
        public abstract void SetIcon(IWindowIconImpl icon);


        public void SetCursor(IPlatformHandle cursor)
        {
            _window.GdkWindow.Cursor = cursor != null ? new Gdk.Cursor(cursor.Handle) : DefaultCursor;
        }

        public void Show() => _window.Show();

        public void Hide() => _window.Hide();
        public abstract void BeginMoveDrag();
        public abstract void BeginResizeDrag(WindowEdge edge);
        public abstract Point Position { get; set; }

        void ITopLevelImpl.Activate()
        {
            _window.Activate();
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

        void OnButtonPressEvent(object o, Gtk.ButtonPressEventArgs args)
        {
            var evnt = args.Event;
            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                _inputRoot,
                evnt.Button == 1
                    ? RawMouseEventType.LeftButtonDown
                    : evnt.Button == 3 ? RawMouseEventType.RightButtonDown : RawMouseEventType.MiddleButtonDown,
                new Point(evnt.X, evnt.Y), GetModifierKeys(evnt.State));
            Input(e);
        }

        void OnScrollEvent(object o, Gtk.ScrollEventArgs args)
        {
            var evnt = args.Event;
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
        }

        protected void OnButtonReleaseEvent(object o, Gtk.ButtonReleaseEventArgs args)
        {
            var evnt = args.Event;
            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                _inputRoot,
                evnt.Button == 1
                    ? RawMouseEventType.LeftButtonUp
                    : evnt.Button == 3 ? RawMouseEventType.RightButtonUp : RawMouseEventType.MiddleButtonUp,
                new Point(evnt.X, evnt.Y), GetModifierKeys(evnt.State));
            Input(e);
        }

        void OnDestroyed(object sender, EventArgs eventArgs)
        {
            Closed();
        }

        private void ProcessKeyEvent(EventKey evnt)
        {
            
            _lastKeyEventTimestamp = evnt.Time;
            if (_imContext.FilterKeypress(evnt))
                return;
            var e = new RawKeyEventArgs(
                GtkKeyboardDevice.Instance,
                evnt.Time,
                evnt.Type == EventType.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                GtkKeyboardDevice.ConvertKey(evnt.Key), GetModifierKeys(evnt.State));
            Input(e);
        }

        void OnKeyPressEvent(object o, Gtk.KeyPressEventArgs args)
        {
            args.RetVal = true;
            ProcessKeyEvent(args.Event);
        }

        void OnKeyReleaseEvent(object o, Gtk.KeyReleaseEventArgs args)
        {
            args.RetVal = true;
            ProcessKeyEvent(args.Event);
        }

        private void ImContext_Commit(object o, Gtk.CommitArgs args)
        {
            Input(new RawTextInputEventArgs(GtkKeyboardDevice.Instance, _lastKeyEventTimestamp, args.Str));
        }

        void OnExposeEvent(object o, Gtk.ExposeEventArgs args)
        {
            Paint(args.Event.Area.ToAvalonia());
            args.RetVal = true;
        }

        void OnMotionNotifyEvent(object o, Gtk.MotionNotifyEventArgs args)
        {
            var evnt = args.Event;
            var position = new Point(evnt.X, evnt.Y);

            GtkMouseDevice.Instance.SetClientPosition(position);

            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                _inputRoot,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt.State));
            Input(e);
            args.RetVal = true;
        }

        public void SetCoverTaskbarWhenMaximized(bool enable)
        {
            // No action neccesary on Gtk.
        }

        public void Dispose()
        {
            _window.Hide();
            _window.Dispose();
            _window = null;
        }
    }
}
