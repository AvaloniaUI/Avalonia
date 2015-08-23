// -----------------------------------------------------------------------
// <copyright file="WindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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

        public WindowImpl()
            : base(Gtk.WindowType.Toplevel)
        {
            this.DefaultSize = new Gdk.Size(640, 480);
            this.Events = Gdk.EventMask.PointerMotionMask | 
                          Gdk.EventMask.ButtonPressMask | 
                          Gdk.EventMask.ButtonReleaseMask;
            this.windowHandle = new PlatformHandle(this.Handle, "GtkWindow");
        }

        public WindowImpl(Gtk.WindowType type)
            : base(type)
        {
            this.Events = Gdk.EventMask.PointerMotionMask |
                          Gdk.EventMask.ButtonPressMask |
                          Gdk.EventMask.ButtonReleaseMask;
            this.windowHandle = new PlatformHandle(this.Handle, "GtkWindow");
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
            this.Window.GetRootOrigin(out x, out y);
            
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

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            var e = new RawMouseEventArgs(
                GtkMouseDevice.Instance,
                evnt.Time,
                this.owner,
                RawMouseEventType.LeftButtonDown,
                new Point(evnt.X, evnt.Y));
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
                new Point(evnt.X, evnt.Y));
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

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            var e = new RawKeyEventArgs(
                GtkKeyboardDevice.Instance,
                evnt.Time,
                RawKeyEventType.KeyDown,
                GtkKeyboardDevice.ConvertKey(evnt.Key),
                new string((char)Gdk.Keyval.ToUnicode((uint)evnt.Key), 1));
            this.Input(e);
            return true;
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
                position);
            this.Input(e);
            return true;
        }

        private IPlatformHandle GetHandle(Gdk.Window window)
        {
            return new PlatformHandle(window.Handle, "GdkWindow");
        }
    }
}