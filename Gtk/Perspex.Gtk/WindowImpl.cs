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

    public class WindowImpl : IWindowImpl
	{
		private Gtk.Window inner;

        private Window owner;

		public WindowImpl ()
		{
            this.inner = new Gtk.Window(Gtk.WindowType.Toplevel);
            this.inner.DefaultSize = new Gdk.Size(640, 480);
            this.inner.FocusActivated += (s, a) => this.Activated();
            this.inner.Destroyed += (s, a) => this.Closed();
            this.inner.ConfigureEvent += (s, a) => this.Resized(new Size(a.Event.Width, a.Event.Height));
            this.inner.ExposeEvent += (s, a) => this.Paint(a.Event.Area.ToPerspex(), GetHandle(a.Event.Window));

            this.Handle = new PlatformHandle(this.inner.Handle, "GtkWindow");
        }

        public Size ClientSize
        {
            get
            {
                int width;
                int height;
                this.inner.GetSize(out width, out height);
                return new Size(width, height);
            }
        }

        public IPlatformHandle Handle
        {
            get;
            private set;
        }

        public Action Activated { get; set; }

        public Action Closed { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Action<Rect, IPlatformHandle> Paint { get; set; }

        public Action<Size> Resized { get; set; }

        public void Invalidate(Rect rect)
        {
            this.inner.QueueDraw();
        }

        public void SetOwner(Window window)
        {
            this.owner = window;
        }

        public void SetTitle(string title)
        {
            this.inner.Title = title;
        }

        public void Show()
        {
            this.inner.Show();
        }

        private IPlatformHandle GetHandle(Gdk.Window window)
        {
            return new PlatformHandle(window.Handle, "GdkWindow");
        }
    }
}