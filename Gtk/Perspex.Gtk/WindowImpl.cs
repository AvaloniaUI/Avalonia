// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Gtk
{
    using System;
    using Perspex.Controls;
    using Perspex.Input.Raw;
    using Perspex.Platform;
    using Gtk = global::Gtk;

    public class WindowImpl : Gtk.Window, IWindowImpl
	{
        private IPlatformHandle windowHandle;

        private Size clientSize;

		public WindowImpl()
            : base(Gtk.WindowType.Toplevel)
		{
            this.DefaultSize = new Gdk.Size(640, 480);
            this.windowHandle = new PlatformHandle(this.Handle, "GtkWindow");
        }

        public Size ClientSize
        {
            get { return this.clientSize; }
        }

        IPlatformHandle IWindowImpl.Handle
        {
            get { return this.windowHandle; }
        }

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect, IPlatformHandle> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public void Invalidate(Rect rect)
        {
            this.QueueDraw();
        }

        public void SetOwner(Window window)
        {
        }

        public void SetTitle(string title)
        {
            this.Title = title;
        }

        protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
        {
            var newSize = new Size(evnt.Width, evnt.Height);

            if (newSize != this.clientSize)
            {
                this.clientSize = newSize;
                this.Resized(clientSize);
            }

            return true;
        }

        protected override void OnDestroyed()
        {
            this.Closed();
        }

        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            this.Paint(evnt.Area.ToPerspex(), GetHandle(evnt.Window));
            return true;
        }

        protected override void OnFocusActivated()
        {
            this.Activated();
        }

        private IPlatformHandle GetHandle(Gdk.Window window)
        {
            return new PlatformHandle(window.Handle, "GdkWindow");
        }
    }
}