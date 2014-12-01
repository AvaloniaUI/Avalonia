// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Gtk
{
	using Perspex.Input;
	using Perspex.Rendering;
	using Perspex.Layout;
	using Perspex.Controls;
	using Gtk = global::Gtk;

	public class Window : ContentControl, ILayoutRoot, IRenderRoot, ICloseable
	{
		public static readonly PerspexProperty<string> TitleProperty = 
			PerspexProperty.Register<Window, string>("Title");

		private Gtk.Window inner;

		public Window ()
		{
			this.inner = new Gtk.Window(Gtk.WindowType.Toplevel);
			inner.SetDefaultSize(1400, 800);
			inner.SetPosition(Gtk.WindowPosition.Center);
			inner.DeleteEvent += delegate { Gtk.Application.Quit(); };
		}

		public event System.EventHandler Closed;

		public Size ClientSize 
		{
			get { throw new System.NotImplementedException (); }
		}

		public ILayoutManager LayoutManager {
			get { throw new System.NotImplementedException (); }
		}

		public IRenderManager RenderManager {
			get { throw new System.NotImplementedException (); }
		}

		public string Title
		{
			get { return this.GetValue(TitleProperty); }
			set { this.SetValue(TitleProperty, value); }
		}

		public void Show()
		{
			this.inner.Show();
		}
	}
}