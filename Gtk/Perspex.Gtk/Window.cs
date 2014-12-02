// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Gtk
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
	using Perspex.Input;
    using Perspex.Layout;
	using Perspex.Rendering;
    using Perspex.Platform;
    using Perspex.Threading;
    using Splat;
	using Gtk = global::Gtk;

	public class Window : ContentControl, ILayoutRoot, IRenderRoot, ICloseable
	{
		public static readonly PerspexProperty<string> TitleProperty = 
			PerspexProperty.Register<Window, string>("Title");

		private Gtk.Window inner;

        private Dispatcher dispatcher;

        private IRenderer renderer;

		public Window ()
		{
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();

            this.inner = new Gtk.Window(Gtk.WindowType.Toplevel);
			inner.SetDefaultSize(1400, 800);
			inner.SetPosition(Gtk.WindowPosition.Center);

            inner.DeleteEvent += (o, a) =>
            {
                if (this.Closed != null)
                {
                    this.Closed(this, EventArgs.Empty);
                }
            };

            var clientSize = this.ClientSize;
            this.renderer = factory.CreateRenderer(this.inner.Handle, (int)clientSize.Width, (int)clientSize.Height);
            this.LayoutManager = new LayoutManager(this);
            this.RenderManager = new RenderManager();
            this.Template = ControlTemplate.Create<Window>(this.DefaultTemplate);

            this.LayoutManager.LayoutNeeded.Subscribe(x =>
            {
                this.dispatcher.InvokeAsync(
                    () =>
                    {
                        this.LayoutManager.ExecuteLayoutPass();
                        this.renderer.Render(this);
                        this.RenderManager.RenderFinished();
                    },
                    DispatcherPriority.Render);
            });

            this.RenderManager.RenderNeeded
                .Where(_ => !this.LayoutManager.LayoutQueued)
                .Subscribe(x =>
            {
                this.dispatcher.InvokeAsync(
                    () =>
                    {
                        if (!this.LayoutManager.LayoutQueued)
                        {
                            this.renderer.Render(this);
                            this.RenderManager.RenderFinished();
                        }
                    },
                    DispatcherPriority.Render);
            });
        }

		public event System.EventHandler Closed;

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

		public ILayoutManager LayoutManager
        {
            get;
            private set;
		}

		public IRenderManager RenderManager
        {
            get;
            private set;
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

        private Control DefaultTemplate(Window c)
        {
            Border border = new Border();
            border.Background = new Perspex.Media.SolidColorBrush(0xffffffff);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.Bind(
                ContentPresenter.ContentProperty, 
                this.GetObservable(Window.ContentProperty),
                BindingPriority.Style);
            border.Content = contentPresenter;
            return border;
        }
    }
}