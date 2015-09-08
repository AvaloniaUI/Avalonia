// -----------------------------------------------------------------------
// <copyright file="WindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Gdk;
using Perspex.Input;

namespace Perspex.Gtk
{
    using System;
    using System.Threading.Tasks;
    using Perspex.Controls;
    using Perspex.Input.Raw;
    using Perspex.Platform;
    using Gtk = global::Gtk;
    using System.Reactive.Disposables;

    public class WindowImpl : Gtk.Window, IWindowImpl
    {
        private TopLevel owner;

        private IPlatformHandle windowHandle;

        private Size clientSize;

        private Gtk.IMContext imContext;

        private uint lastKeyEventTimestamp;

        private static readonly Gdk.Cursor DefaultCursor = new Gdk.Cursor(CursorType.LeftPtr);

        public WindowImpl()
            : base(Gtk.WindowType.Toplevel)
        {
            this.DefaultSize = new Gdk.Size(640, 480);
            Init();
        }

        public WindowImpl(Gtk.WindowType type)
            : base(type)
        {
            Init();
        }

        private void Init()
        {
            this.Events = Gdk.EventMask.PointerMotionMask |
              Gdk.EventMask.ButtonPressMask |
              Gdk.EventMask.ButtonReleaseMask;
            this.windowHandle = new PlatformHandle(this.Handle, "GtkWindow");
            this.imContext =  new Gtk.IMMulticontext();
            this.imContext.Commit += ImContext_Commit;
        }

        public Size ClientSize
        {
            get;
            set;
        }

        IPlatformHandle ITopLevelImpl.Handle
        {
            get { return this.windowHandle; }
        }

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect, IPlatformHandle> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public void Invalidate(Rect rect)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            this.Draw(new Gdk.Rectangle { X = (int)rect.X, Y = (int)rect.Y, Width = (int)rect.Width, Height = (int)rect.Height });
#pragma warning restore CS0612 // Type or member is obsolete
        }

        public Point PointToScreen(Point point)
        {
            int x, y;
            this.GdkWindow.GetDeskrelativeOrigin(out x, out y);
            
            return new Point(point.X + x, point.Y + y);
        }

        public void SetOwner(TopLevel owner)
        {
            this.owner = owner;
        }

        public void SetTitle(string title)
        {
            this.Title = title;
        }


        public void SetCursor(IPlatformHandle cursor)
        {
            GdkWindow.Cursor = cursor != null ? new Gdk.Cursor(cursor.Handle) : DefaultCursor;
        }

        public IDisposable ShowDialog()
        {
            this.Modal = true;
            this.Show();

            return Disposable.Empty;
        }

        void ITopLevelImpl.Activate()
        {
            this.Activate();
        }

        private static ModifierKeys GetModifierKeys(ModifierType state)
        {
            var rv = ModifierKeys.None;
            if (state.HasFlag(ModifierType.ControlMask))
                rv |= ModifierKeys.Control;
            if (state.HasFlag(ModifierType.ShiftMask))
                rv |= ModifierKeys.Shift;
            if (state.HasFlag(ModifierType.Mod1Mask))
                rv |= ModifierKeys.Control;

            return rv;
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                this.owner,
                RawMouseEventType.LeftButtonDown,
                new Point(evnt.X, evnt.Y), GetModifierKeys(evnt.State));
            this.Input(e);
            return true;
        }

        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                this.owner,
                RawMouseEventType.LeftButtonUp,
                new Point(evnt.X, evnt.Y), GetModifierKeys(evnt.State));
            this.Input(e);
            return true;
        }

        protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
        {
            var newSize = new Size(evnt.Width, evnt.Height);
            
            if (newSize != this.clientSize)
            {
                this.Resized(newSize);
            }

            return true;
        }

        protected override void OnDestroyed()
        {
            this.Closed();
        }

        private bool ProcessKeyEvent(Gdk.EventKey evnt)
        {
            this.lastKeyEventTimestamp = evnt.Time;
            if (this.imContext.FilterKeypress(evnt))
                return true;
            var e = new RawKeyEventArgs(
                GtkKeyboardDevice.Instance,
                evnt.Time,
                evnt.Type == EventType.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                GtkKeyboardDevice.ConvertKey(evnt.Key), GetModifierKeys(evnt.State));
            this.Input(e);
            return true;
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt) => ProcessKeyEvent(evnt);

        protected override bool OnKeyReleaseEvent(EventKey evnt) => ProcessKeyEvent(evnt);

        private void ImContext_Commit(object o, Gtk.CommitArgs args)
        {
            this.Input(new RawTextInputEventArgs(GtkKeyboardDevice.Instance, this.lastKeyEventTimestamp, args.Str));
        }

        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            this.Paint(evnt.Area.ToPerspex(), this.GetHandle(evnt.Window));
            return true;
        }

        protected override void OnFocusActivated()
        {
            this.Activated();
        }

        protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
        {
            var position = new Point(evnt.X, evnt.Y);

            GtkMouseDevice.Instance.SetClientPosition(position);

            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                this.owner,
                RawMouseEventType.Move,
                position, GetModifierKeys(evnt.State));
            this.Input(e);
            return true;
        }

        private IPlatformHandle GetHandle(Gdk.Window window)
        {
            return new PlatformHandle(window.Handle, "GdkWindow");
        }
    }
}