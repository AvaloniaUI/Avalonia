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

            // TODO: Use ?. operator on these when it's available.
            this.inner.FocusActivated += (s, a) => this.Activated.Invoke(this, EventArgs.Empty);
            this.inner.Destroyed += (s, a) => this.Closed.Invoke(this, EventArgs.Empty);
            this.inner.ConfigureEvent += (s, a) => this.Resized.Invoke(this, new RawSizeEventArgs(a.Event.Width, a.Event.Height));

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

        public event EventHandler Activated;

        public event EventHandler Closed;

        public event EventHandler<RawInputEventArgs> Input;

        public event EventHandler<RawSizeEventArgs> Resized;

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
    }
}