// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Gdk;
using Perspex.Controls;
using Perspex.Input.Raw;
using Perspex.Platform;
using Perspex.Input;
using Perspex.Threading;
using Action = System.Action;
using WindowEdge = Perspex.Controls.WindowEdge;

namespace Perspex.Gtk
{
    using Gtk = global::Gtk;

    public class WindowImpl : Gtk.Window, IWindowImpl, IPlatformHandle
    {
        private IInputRoot _inputRoot;
        
        private Size _clientSize;

        private Gtk.IMContext _imContext;

        private uint _lastKeyEventTimestamp;

        private static readonly Gdk.Cursor DefaultCursor = new Gdk.Cursor(CursorType.LeftPtr);

        public WindowImpl()
            : base(Gtk.WindowType.Toplevel)
        {
            DefaultSize = new Gdk.Size(900, 480);
            Init();
        }

        public WindowImpl(Gtk.WindowType type)
            : base(type)
        {
            Init();
        }

        private void Init()
        {
            Events = EventMask.PointerMotionMask |
              EventMask.ButtonPressMask |
              EventMask.ButtonReleaseMask;
            _imContext = new Gtk.IMMulticontext();
            _imContext.Commit += ImContext_Commit;
            DoubleBuffered = false;
            Realize();
        }

		protected override void OnRealized ()
		{
			base.OnRealized ();
			_imContext.ClientWindow = this.GdkWindow;
		}

        public Size ClientSize
        {
            get;
            set;
        }

        public Size MaxClientSize
        {
            get
            {
                // TODO: This should take into account things such as taskbar and window border
                // thickness etc.
                return new Size(Screen.Width, Screen.Height);
            }
        }

        IPlatformHandle ITopLevelImpl.Handle => this;

        [DllImport("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
        extern static IntPtr gdk_win32_drawable_get_handle(IntPtr gdkWindow);

        [DllImport("libgtk-x11-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
        extern static IntPtr gdk_x11_drawable_get_xid(IntPtr gdkWindow);

        [DllImport("libgdk-quartz-2.0-0.dylib", CallingConvention = CallingConvention.Cdecl)]
        extern static IntPtr gdk_quartz_window_get_nswindow(IntPtr gdkWindow);

        IntPtr _nativeWindow;

        IntPtr GetNativeWindow()
        {
            IntPtr h = GdkWindow.Handle;
            if (_nativeWindow != IntPtr.Zero)
                return _nativeWindow;
            //Try whatever backend that works
            try
            {
                return _nativeWindow = gdk_quartz_window_get_nswindow(h);
            }
            catch
            {
            }
            try
            {
                return _nativeWindow = gdk_x11_drawable_get_xid(h);
            }
            catch
            {
            }
            return _nativeWindow = gdk_win32_drawable_get_handle(h);
        }


        IntPtr IPlatformHandle.Handle => GetNativeWindow();
        public string HandleDescriptor => "HWND";

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size> Resized { get; set; }

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

        public Point PointToScreen(Point point)
        {
            int x, y;
            GdkWindow.GetDeskrelativeOrigin(out x, out y);

            return new Point(point.X + x, point.Y + y);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public void SetTitle(string title)
        {
            Title = title;
        }


        public void SetCursor(IPlatformHandle cursor)
        {
            GdkWindow.Cursor = cursor != null ? new Gdk.Cursor(cursor.Handle) : DefaultCursor;
        }

        public void BeginMoveDrag()
        {
            int x, y;
            ModifierType mod;
            Screen.RootWindow.GetPointer(out x, out y, out mod);
            BeginMoveDrag(1, x, y, 0);
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            int x, y;
            ModifierType mod;
            Screen.RootWindow.GetPointer(out x, out y, out mod);
            BeginResizeDrag((Gdk.WindowEdge) (int) edge, 1, x, y, 0);
        }

        public Point Position
        {
            get
            {
                int x, y;
                GetPosition(out x, out y);
                return new Point(x, y);
            }
            set
            {
                Move((int)value.X, (int)value.Y);
            }
        }

        public IDisposable ShowDialog()
        {
            Modal = true;
            Show();

            return Disposable.Empty;
        }

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

            if (newSize != _clientSize)
            {
                Resized(newSize);
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
            Paint(evnt.Area.ToPerspex());
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

        private IPlatformHandle GetHandle(Gdk.Window window)
        {
            return new PlatformHandle(window.Handle, "GdkWindow");
        }
    }
}
